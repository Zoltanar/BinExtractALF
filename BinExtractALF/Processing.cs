using System.Runtime.InteropServices;
using System.Text;
using EushullyExtractionUtils;

// ReSharper disable MustUseReturnValue

namespace BinExtractALF;

public static class Processing
{
    internal static bool Run(string[] args)
    {
        if (args.Length < 1)
        {
            ConsoleUtils.PrintError($@"usage: {nameof(BinExtractALF)} <inputFile> [<outputFolder>]
inputFile: the header/directory file path,
outputFolder: optional output directory where files will be extracted to. 

Examples:
BinExtractALF SYS4INI.BIN
BinExtractALF APPEND01.AAI Output
BinExtractALF SYS5INI.BIN ""..\Extractor Output""

The archived data (.ALF) files must be present in the same folder as the header/directory (.INI or .AAI) file.");
            return false;
        }

        var inputFile = args[0];
        if (!File.Exists(inputFile))
        {
            ConsoleUtils.PrintError($"File {new FileInfo(inputFile).FullName} not found.");
            return false;
        }
        ConsoleUtils.PrintWarning($"Processing, input file: {new FileInfo(inputFile).FullName}");
        var outputDirectory = args.Length > 1 ? args[1] : null;
        ConsoleUtils.PrintWarning($"Processing, output directory: {outputDirectory ?? "(not specified)"}");
        var fileStream = OpenFile(inputFile, FileMode.Open);
        return ProcessFile(inputFile, outputDirectory, fileStream);
    }

    public static bool ProcessFile(string inputFile, string? outputDirectory, FileStream fileStream)
    {
        GetFileInformation(fileStream, out var archiveCount, out var archiveEntries, out var fileCount, out var fileEntries);
        var archiveItems = new ArchiveInfo[archiveCount];
        var inputDirectory = Directory.GetParent(inputFile)!;
        for (uint i = 0; i < archiveCount; i++)
        {
            archiveItems[i].FileName = Path.Combine(inputDirectory.FullName, archiveEntries[i]);
            if (File.Exists(archiveItems[i].FileName))
            {
                archiveItems[i].OutputDirectory = Path.GetFileNameWithoutExtension(archiveEntries[i]) + '/';
                if (outputDirectory != null) archiveItems[i].OutputDirectory = Path.Combine(outputDirectory, archiveItems[i].OutputDirectory);
                Directory.CreateDirectory(archiveItems[i].OutputDirectory);
            }
            else
            {
                ConsoleUtils.PrintError($"{archiveEntries[i]}: could not open (skipped!)\n");
                archiveItems[i].FileName = null;
            }
        }

        for (uint i = 0; i < fileCount; i++)
        {
            ArchiveInfo archive = archiveItems[fileEntries[i].ArchiveIndex];

            if (archive.FileName == null || fileEntries[i].Length == 0) continue;
            uint len = fileEntries[i].Length;
            var buff = new byte[len];
            var file = OpenFile(archive.FileName, FileMode.Open);
            file.Seek(fileEntries[i].Offset, SeekOrigin.Begin);
            file.Read(buff, 0, (int)len);
            var outputFile = OpenFile(archive.OutputDirectory + fileEntries[i].FileName, FileMode.OpenOrCreate);
            outputFile.Write(buff);
            outputFile.Dispose();
            file.Dispose();
        }
        return true;
    }

    public static void GetFileInformation(Stream stream, 
        out int archiveCount, out string[] archiveEntries, 
        out int fileCount, out IFileEntry[] fileEntries)
    {
        bool isS5 = TryS5(stream);
        var hdr = GetHeader(stream, isS5);
        //different size header for S4 append archives
        if (hdr.Signature.StartsWith("S4AC")) stream.Seek(268, SeekOrigin.Begin);
        byte[] data;
        if (!isS5)
        {
            data = isS5 ? ReadSector<S5SECTHDR>(stream) : ReadSector<S4SECTHDR>(stream);
        }
        else
        {
            data = new byte[stream.Length - stream.Position];
            stream.Read(data, 0, data.Length);
        }
        stream.Dispose();
        archiveCount = (int)BitConverter.ToUInt32(data, 0);
        var offset = 4;
        var sizeOfArcEntries = (isS5 ? GetSize<S5TOCARCENTRY>() : GetSize<S4TOCARCENTRY>()) * archiveCount;
        archiveEntries = (isS5 ?
                Operation.ByteArrayToStructureArray<S5TOCARCENTRY>(data, offset, sizeOfArcEntries).Cast<ITOCARCENTRY>() :
                Operation.ByteArrayToStructureArray<S4TOCARCENTRY>(data, offset, sizeOfArcEntries).Cast<ITOCARCENTRY>())
            .Select(i => i.GetFilename()).ToArray();
        offset += sizeOfArcEntries;
        if (isS5) offset += 256;
        ConsoleUtils.PrintWarning($"Found {archiveCount} archives...");
        fileCount = (int)BitConverter.ToUInt32(data, offset);
        offset += 4;
        var sizeOfFileEntries = fileCount * (isS5 ? GetSize<S5FileEntry>() : GetSize<S4FileEntry>());
        fileEntries = (isS5 ? Operation.ByteArrayToStructureArray<S5FileEntry>(data, offset, sizeOfFileEntries).Cast<IFileEntry>()
                : Operation.ByteArrayToStructureArray<S4FileEntry>(data, offset, sizeOfFileEntries).Cast<IFileEntry>()
            ).ToArray();
        ConsoleUtils.PrintWarning($"Found {fileCount} files within archives...");
    }

    private static IHeader GetHeader(Stream fd, bool isS5)
    {
        IHeader hdr;
        if (isS5)
        {
            Operation.ReadToStructure(fd, out S5HDR s5Hdr, GetSize<S5HDR>());
            hdr = s5Hdr;
        }
        else
        {
            Operation.ReadToStructure(fd, out S4HDR s4Hdr, GetSize<S4HDR>());
            hdr = s4Hdr;
        }

        return hdr;
    }
    private static byte[] ReadSector<T>(Stream stream) where T : struct, ISectorHeader
    {
        Operation.ReadToStructure(stream, out T hdr, GetSize<T>());
        var len = hdr.Length;
        var buff = new byte[len];
        stream.Read(buff, 0, (int)len);
        return hdr.OriginalLength == hdr.Length ? buff : Compression.UnpackLZSS(buff, (int)hdr.OriginalLength);
    }

    private static bool TryS5(Stream fileStream)
    {
        var buff = new byte[480];
        fileStream.Read(buff, 0, buff.Length);
        var text = Encoding.Unicode.GetString(buff, 0, 8);
        fileStream.Seek(-buff.Length, SeekOrigin.Current);
        return text.StartsWith("S5");
    }

    private static int GetSize<T>() where T : struct
    {
        var item = new T();
        if (item is IFromBytes fromBytes) return fromBytes.Size;
        return Marshal.SizeOf<T>();
    }

    public static FileStream OpenFile(string filename, FileMode fileMode)
    {
        Directory.CreateDirectory(Directory.GetParent(filename)!.FullName);
        var fileStream = File.Open(filename, fileMode);
        return fileStream;
    }
}