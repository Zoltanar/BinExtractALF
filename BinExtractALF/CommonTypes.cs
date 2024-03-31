// ReSharper disable InconsistentNaming
namespace BinExtractALF
{
    public interface IHeader
    {
        string Signature { get; }
    }

    interface ISectorHeader
    {
        ulong OriginalLength { get; }
        ulong Length { get; }
    }

    public interface ITOCARCENTRY
    {
        string GetFilename();
    }

    public interface IFileEntry
    {
        public string FileName { get; }
        public uint ArchiveIndex { get; }
        public uint FileIndex { get; }
        public uint Offset { get; }
        public uint Length { get; }
    }
    
    public struct ArchiveInfo
    {
        public string? FileName;
        public string OutputDirectory;
    }
}
