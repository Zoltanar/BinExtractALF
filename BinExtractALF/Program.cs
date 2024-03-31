﻿// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AGF2BMP2AGF;
using BinExtractALF;

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


unsafe bool Run(string[] args)
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
    IHeader hdr = null;
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
    byte[] toc_buff = null;
    if (!isS5)
    {
        toc_buff = isS5 ? ReadSector<S5SECTHDR>(fd) : ReadSector<S4SECTHDR>(fd);
    }
    else
    {
        toc_buff = new byte[fd.Length - fd.Position];
        fd.Read(toc_buff, 0, toc_buff.Length);
    }
    fd.Dispose();
    S4TOCARCHDR archdr = Operation.ByteArrayToStructure<S4TOCARCHDR>(toc_buff);
    var offset = Marshal.SizeOf<S4TOCARCHDR>();
    var sizeOfArcEntries = (isS5 ? Marshal.SizeOf<S5TOCARCENTRY>() : Marshal.SizeOf<S4TOCARCENTRY>()) * (int)archdr.entry_count;
    string[] arcentries = (isS5 ?
            GetS5EntryNames(toc_buff, offset, sizeOfArcEntries) :
        Operation.ByteArrayToStructureArray<S4TOCARCENTRY>(toc_buff, offset, sizeOfArcEntries).Cast<ITOCARCENTRY>()
        .Select(i => new SafeTOCARCENTRY(i).FileName).ToArray());
    offset += sizeOfArcEntries;
    S4TOCARCHDR filhdr = Operation.ByteArrayToStructure<S4TOCARCHDR>(toc_buff, offset);
    offset += Marshal.SizeOf<S4TOCARCHDR>();
    var sizeOfFileEntries = (int)filhdr.entry_count * Marshal.SizeOf<S4TOCFILENTRY>();
    S4TOCFILENTRY[] filentries = Operation.ByteArrayToStructureArray<S4TOCFILENTRY>(toc_buff, offset, sizeOfFileEntries);

    arc_info_t[] arc_info = new arc_info_t[archdr.entry_count];

    var inputDirectory = Directory.GetParent(args[1]);
    for (uint i = 0; i < archdr.entry_count; i++)
    {
        arc_info[i].fd =  Path.Combine(inputDirectory.FullName, arcentries[i]);
        if (File.Exists(arc_info[i].fd))
        {
            arc_info[i].dir = Path.GetFileNameWithoutExtension(arcentries[i]) + '/';
            if(args.Length >= 3) arc_info[i].dir = Path.Combine(args[2], arc_info[i].dir);
            Directory.CreateDirectory(arc_info[i].dir);
        }
        else
        {
            PrintError($"{arcentries[i]}: could not open (skipped!)\n");
            arc_info[i].fd = null;
        }
    }

    for (uint i = 0; i < filhdr.entry_count; i++)
    {
        arc_info_t arc = arc_info[filentries[i].archive_index];

        if (arc.fd == null || filentries[i].length == 0)
        {
            continue;
        }
        uint len = filentries[i].length;
        var buff = new byte[len];
        var file = Algorithm.OpenFileOrDie(arc.fd, FileMode.Open);
        file.Seek(filentries[i].offset, SeekOrigin.Begin);
        file.Read(buff, 0, (int)len);
        var out_fd = Algorithm.OpenFileOrDie(arc.dir + filentries[i].GetFilename(), FileMode.OpenOrCreate);
        out_fd.Write(buff);
        out_fd.Dispose();
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
        file.Seek(entry.Bytes2, SeekOrigin.Begin);
        file.Read(buff, 0, (int)len);

        var out_fd = Algorithm.OpenFileOrDie("Hyakusen-Data/" + entry.FileName, FileMode.OpenOrCreate);
        out_fd.Write(buff);
        out_fd.Dispose();
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
