using System.Runtime.InteropServices;
using System.Text;
using EushullyExtractionUtils;

// ReSharper disable InconsistentNaming

namespace BinExtractALF
{
    //most of this ported from

    // exs4alf.cpp, v1.1 2009/04/26
    // coded by asmodean

    // contact: 
    //   web:   http://asmodean.reverse.net
    //   email: asmodean [at] hush.com
    //   irc:   asmodean on efnet (irc.efnet.net)


    public struct S4HDR : IHeader, IFromBytes
    {
        public int Size => 240 + 60;

        /// <example>S4IC413 [title]</example>
        /// <example>S4AC422 [title]</example>
        private string _signature;
        public byte[] Unknown;


        public void GetFromBytes(byte[] bytes, int offset)
        {
            _signature = Encoding.UTF8.GetString(bytes, offset, 240).TrimEnd('\0');
            Unknown = bytes.Skip(offset+240).Take(60).ToArray();
        }

        public string Signature => _signature;

        public override string ToString() => _signature;
    }
    
    public struct S4SECTHDR : ISectorHeader, IFromBytes
    {
        public int Size => 12;
        public ulong OriginalLength { get; set; }
        public ulong OriginalLength2 { get; set; }
        public ulong Length { get; set; }

        public void GetFromBytes(byte[] data, int offset)
        {
            OriginalLength = BitConverter.ToUInt32(data, offset);
            OriginalLength2 = BitConverter.ToUInt32(data, offset + 4);
            Length = BitConverter.ToUInt32(data, offset + 8);
        }
    }

    public struct S4TOCARCENTRY : ITOCARCENTRY, IFromBytes
    {
        // There's a bunch of junk following the name which I assume is
        // uninitialized memory...
        public string FileName { get; set; }

        public int Size => 256;

        public void GetFromBytes(byte[] data, int offset)
        {
            FileName = Encoding.UTF8.GetString(data, offset, Size).TrimEnd('\0');

            var firstNullCharacter = FileName.IndexOf('\0');
            FileName = FileName.Substring(0, firstNullCharacter);

        }

        public string GetFilename() => FileName;

        public override string ToString() => FileName;
    }
    
    public struct S4FileEntry : IFileEntry, IFromBytes
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
        public int Size => 80;

        public void GetFromBytes(byte[] data, int offset)
        {
            FileName = Encoding.UTF8.GetString(data, offset, 64).TrimEnd('\0');
            ArchiveIndex = BitConverter.ToUInt32(data, offset + 64);
            FileIndex = BitConverter.ToUInt32(data, offset + 68);
            Offset = BitConverter.ToUInt32(data, offset + 72);
            Length = BitConverter.ToUInt32(data, offset + 76);
        }

        public override string ToString() => $"{FileName}@{ArchiveIndex}:{Offset}, size: {Length}";
    };
    

}
