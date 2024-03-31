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
        Algorithm.ReadToStructure(fd, out S5HDR s5Hdr, GetSize<S5HDR>());
        hdr = s5Hdr;
    }
    else
    {
        Algorithm.ReadToStructure(fd, out S4HDR s4Hdr, GetSize<S4HDR>());
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
    var offset = GetSize<S4TOCARCHDR>();
    var sizeOfArcEntries = (isS5 ? GetSize<S5TOCARCENTRY>() : GetSize<S4TOCARCENTRY>()) * (int)archivesHeader.entry_count;
    string[] archiveEntries = (isS5 ?
        Operation.ByteArrayToStructureArray<S5TOCARCENTRY>(data, offset, sizeOfArcEntries).Cast<ITOCARCENTRY>() :
        Operation.ByteArrayToStructureArray<S4TOCARCENTRY>(data, offset, sizeOfArcEntries).Cast<ITOCARCENTRY>())
        .Select(i => i.GetFilename()).ToArray();
    if (isS5) offset = 1060 - 544;
    else offset += sizeOfArcEntries;
    PrintWarning($"Found {archivesHeader.entry_count} archives...");
    S4TOCARCHDR filesHeader = Operation.ByteArrayToStructure<S4TOCARCHDR>(data, offset);
    offset += GetSize<S4TOCARCHDR>();
    var sizeOfFileEntries = (int)filesHeader.entry_count * (isS5 ? GetSize<S5FileEntry>() : GetSize<S4FileEntry>());
    IFileEntry[] fileEntries = (isS5 ? Operation.ByteArrayToStructureArray<S5FileEntry>(data, offset, sizeOfFileEntries).Cast<IFileEntry>() 
        : Operation.ByteArrayToStructureArray<S4FileEntry>(data, offset, sizeOfFileEntries).Cast<IFileEntry>()
        ).ToArray();
    PrintWarning($"Found {filesHeader.entry_count} files within archives...");
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
        ArchiveInfo archive = archiveItems[fileEntries[i].ArchiveIndex];

        if (archive.FileName == null || fileEntries[i].Length == 0)
        {
            continue;
        }
        uint len = fileEntries[i].Length;
        var buff = new byte[len];
        var file = Algorithm.OpenFileOrDie(archive.FileName, FileMode.Open);
        file.Seek(fileEntries[i].Offset, SeekOrigin.Begin);
        file.Read(buff, 0, (int)len);
        var outputFile = Algorithm.OpenFileOrDie(archive.OutputDirectory + fileEntries[i].FileName, FileMode.OpenOrCreate);
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
    Algorithm.ReadToStructure(stream, out T hdr, GetSize<T>());
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

static int GetSize<T>() where T : struct
{
    var item = new T();
    if (item is IFromBytes fromBytes) return fromBytes.Size;
    return Marshal.SizeOf<T>();
}
