using System.Text;
using AGF2BMP2AGF;

// ReSharper disable InconsistentNaming

namespace BinExtractALF
{
    public struct S5HDR : IHeader, IFromBytes
    {
        public int Size => 480 + 60;
        private string _signature;
        public byte[] Unknown;

        public void GetFromBytes(byte[] bytes, int offset)
        {
            _signature = Encoding.Unicode.GetString(bytes, offset, 480).TrimEnd('\0');
            Unknown = bytes.Skip(offset+480).Take(60).ToArray();
        }

        public string Signature => _signature;

        public override string ToString() => Signature;
    }

    public struct S5SECTHDR : ISectorHeader
    {
        public uint original_length;
        public uint length;
        public ulong OriginalLength => original_length;
        public ulong Length => length;
    }

    public struct S5TOCARCENTRY : ITOCARCENTRY, IFromBytes
    {
        public string FileName { get; set; }
        public string GetFilename() => FileName;

        public override string ToString() => FileName;
        public int Size => 256;
        public void GetFromBytes(byte[] data, int offset)
        {
            FileName = Encoding.Unicode.GetString(data, offset, 256).TrimEnd('\0');
            var firstNullCharacter = FileName.IndexOf('\0');
            FileName = FileName.Substring(0, firstNullCharacter);
        }
    }

    //144 total?
    public struct S5FileEntry: IFileEntry, IFromBytes
    {
        /// <remarks>132 bytes</remarks>
        public string FileName { get; set; }

        /// <remarks>4 bytes after file name //maybe archive index (which archive)</remarks>
        public uint ArchiveIndex { get; set; }

        /// <remarks>4 bytes after archive index (index of file within archive?)</remarks>
        public uint FileIndex { get; set; }
        
        /// <summary>
        /// Location in archive (bytes offset)
        /// </summary>
        /// <remarks>4 Bytes after Bytes1</remarks>
        public uint Offset { get; set; }

        /// <summary>
        /// Size of file in bytes
        /// </summary>
        /// <remarks>4 bytes after location</remarks>
        public uint Length { get; set; }

        /// <summary>
        /// Size of this object in bytes
        /// </summary>
        public int Size => 144;

        public void GetFromBytes(byte[] data, int offset)
        {
            FileName = Encoding.Unicode.GetString(data, offset, 128).TrimEnd('\0');
            ArchiveIndex = BitConverter.ToUInt32(data, offset + 128);
            FileIndex = BitConverter.ToUInt32(data, offset + 132);
            Offset = BitConverter.ToUInt32(data, offset + 136);
            Length = BitConverter.ToUInt32(data, offset + 140);
        }

        public override string ToString() => $"{FileName}@{ArchiveIndex}:{Offset}, size: {Length}";
    }
}
