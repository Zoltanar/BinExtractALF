// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AGF2BMP2AGF;
using BinExtractALF;
// ReSharper disable MustUseReturnValue

PrintWarning("BinExtractAlf v0.01, version 4 code based on exs4alf v1.01 by asmodean");
PrintWarning($"Processing: {string.Join(Environment.NewLine, args)}");
var timer = Stopwatch.StartNew();
var success = Run(args);
if (success) Print($"Completed in {timer.Elapsed:g}");
else PrintError($"Completed with errors in {timer.Elapsed:g}");
return;
// exs4alf.cpp, v1.1 2009/04/26
// coded by asmodean

// contact: 
//   web:   http://asmodean.reverse.net
//   email: asmodean [at] hush.com
//   irc:   asmodean on efnet (irc.efnet.net)

// This tool extracts S4IC413 (sys4ini.bin + *.ALF) and S4AC422 (*.AAI + *.ALF)
// archives.


bool Run(string[] args)
{
    var argc = args.Length;
    if (argc < 2)
    {
        PrintError("BinExtractAlf v0.01, version 4 code based on exs4alf v1.01 by asmodean");
        PrintError($"usage: {args[0]} <sys4ini.bin> [<outputFolder>]\n");
        return false;
    }

    if (!File.Exists(args[1]))
    {
        PrintError($"File {new FileInfo(args[1]).FullName} not found.");
        return false;
    }
    var fd = Algorithm.OpenFileOrDie(args[1], FileMode.Open);
    bool isS5 = TryS5(fd);
    IHeader hdr;
    if (isS5)
    {
        Algorithm.ReadToStructure(fd, out S5HDR s5Hdrhdr, Marshal.SizeOf<S5HDR>());
        hdr = s5Hdrhdr;
    }
    else
    {
        Algorithm.ReadToStructure(fd, out S4HDR s4Hdr, Marshal.SizeOf<S4HDR>());
        hdr = s4Hdr;
    }
    //different size header for S4 append archives
    if (hdr.Signature.StartsWith("S4AC")) fd.Seek(268, SeekOrigin.Begin);
    byte[] data;
    if (!isS5)
    {
        data = isS5 ? ReadSector<S5SECTHDR>(fd) : ReadSector<S4SECTHDR>(fd);
    }
    else
    {
        data = new byte[fd.Length - fd.Position];
        fd.Read(data, 0, data.Length);
    }
    fd.Dispose();
    S4TOCARCHDR archivesHeader = Operation.ByteArrayToStructure<S4TOCARCHDR>(data);
    var offset = Marshal.SizeOf<S4TOCARCHDR>();
    var sizeOfArcEntries = (isS5 ? Marshal.SizeOf<S5TOCARCENTRY>() : Marshal.SizeOf<S4TOCARCENTRY>()) * (int)archivesHeader.entry_count;
    string[] archiveEntries = (isS5 ?
            GetS5EntryNames(data, offset, sizeOfArcEntries) :
        Operation.ByteArrayToStructureArray<S4TOCARCENTRY>(data, offset, sizeOfArcEntries).Cast<ITOCARCENTRY>()
        .Select(i => new SafeTOCARCENTRY(i).FileName).ToArray());
    offset += sizeOfArcEntries;
    S4TOCARCHDR filesHeader = Operation.ByteArrayToStructure<S4TOCARCHDR>(data, offset);
    offset += Marshal.SizeOf<S4TOCARCHDR>();
    var sizeOfFileEntries = (int)filesHeader.entry_count * Marshal.SizeOf<S4TOCFILENTRY>();
    S4TOCFILENTRY[] fileEntries = Operation.ByteArrayToStructureArray<S4TOCFILENTRY>(data, offset, sizeOfFileEntries);

    var archiveItems = new ArchiveInfo[archivesHeader.entry_count];

    var inputDirectory = Directory.GetParent(args[1])!;
    for (uint i = 0; i < archivesHeader.entry_count; i++)
    {
        archiveItems[i].FileName =  Path.Combine(inputDirectory.FullName, archiveEntries[i]);
        if (File.Exists(archiveItems[i].FileName))
        {
            archiveItems[i].OutputDirectory = Path.GetFileNameWithoutExtension(archiveEntries[i]) + '/';
            if(args.Length >= 3) archiveItems[i].OutputDirectory = Path.Combine(args[2], archiveItems[i].OutputDirectory);
            Directory.CreateDirectory(archiveItems[i].OutputDirectory);
        }
        else
        {
            PrintError($"{archiveEntries[i]}: could not open (skipped!)\n");
            archiveItems[i].FileName = null;
        }
    }

    for (uint i = 0; i < filesHeader.entry_count; i++)
    {
        ArchiveInfo archive = archiveItems[fileEntries[i].archive_index];

        if (archive.FileName == null || fileEntries[i].length == 0)
        {
            continue;
        }
        uint len = fileEntries[i].length;
        var buff = new byte[len];
        var file = Algorithm.OpenFileOrDie(archive.FileName, FileMode.Open);
        file.Seek(fileEntries[i].offset, SeekOrigin.Begin);
        file.Read(buff, 0, (int)len);
        var outputFile = Algorithm.OpenFileOrDie(archive.OutputDirectory + fileEntries[i].GetFilename(), FileMode.OpenOrCreate);
        outputFile.Write(buff);
        outputFile.Dispose();
        file.Dispose();

    }
    return true;
}

static void PrintError(string text)
{
    AGF2BMP2AGF.Program.Print(AGF2BMP2AGF.Program.ErrorColor, text.TrimEnd('\r', '\n'));
}

static void PrintWarning(string text)
{
    AGF2BMP2AGF.Program.Print(AGF2BMP2AGF.Program.WarningColor, text.TrimEnd('\r', '\n'));
}

static void Print(string text)
{
    AGF2BMP2AGF.Program.Print(AGF2BMP2AGF.Program.SuccessColor, text.TrimEnd('\r', '\n'));
}

static byte[] ReadSector<T>(Stream stream) where T : struct, ISectorHeader
{
    Algorithm.ReadToStructure(stream, out T hdr, Marshal.SizeOf<T>());
    var len = hdr.Length;
    var buff = new byte[len];
    stream.Read(buff, 0, (int)len);
    return hdr.OriginalLength == hdr.Length ? buff : Compression.UnpackLZSS(buff, (int)hdr.OriginalLength);
}

bool TryS5(FileStream fileStream)
{
    var buff = new byte[480];
    fileStream.Read(buff, 0, buff.Length);
    var text = Encoding.Unicode.GetString(buff, 0, 8);
    fileStream.Seek(-buff.Length, SeekOrigin.Current);
    return text.StartsWith("S5");
}



static string[] GetS5EntryNames(byte[] bytes, int offset, int length)
{
    //
    int traveled1 = 0;
    var list1 = new List<SafeS5TOCARCENTRY>();
    int size1 = 144;
    var offset1 = 1060 - 540;
    while (offset1 + traveled1 + size1 < bytes.Length)
    {

        var fileName = new SafeS5TOCARCENTRY(bytes, offset1 + traveled1);
        list1.Add(fileName);
        traveled1 += size1;
    }

    foreach (var entry in list1)
    {
        uint len = entry.FileSize;
        var buff = new byte[len];
        var file = Algorithm.OpenFileOrDie("Data.ALF", FileMode.Open);
        file.Seek(entry.Location, SeekOrigin.Begin);
        file.Read(buff, 0, (int)len);

        var outputFile = Algorithm.OpenFileOrDie("Hyakusen-Data/" + entry.FileName, FileMode.OpenOrCreate);
        outputFile.Write(buff);
        outputFile.Dispose();
        file.Dispose();
    }
    throw new NotSupportedException("WIP S5 mode, ends here.");
    //
    int traveled = 0;
    var list = new List<string>();
    int size = Marshal.SizeOf(typeof(S5TOCARCENTRY));
    while (traveled < length)
    {
        var fileName = Encoding.Unicode.GetString(bytes, offset + traveled, size).TrimEnd('\0');
        list.Add(fileName);
        traveled += size;
    }
    return list.ToArray();
}
