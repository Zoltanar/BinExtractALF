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

    public struct SafeTOCARCENTRY
    {
        public string FileName { get; set; }
        public SafeTOCARCENTRY(ITOCARCENTRY data)
        {
            FileName = data.GetFilename();
        }

        public override string ToString() => FileName;
    }

    public struct ArchiveInfo
    {
        public string? FileName;
        public string OutputDirectory;
    }
}
