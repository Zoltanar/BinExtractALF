using System.Runtime.InteropServices;
using System.Text;
// ReSharper disable InconsistentNaming

namespace BinExtractALF
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct S5HDR : IHeader
    {
        /// <example>S4IC413 [title]</example>
        /// <example>S4AC422 [title]</example>
        public fixed byte signature_title[480]; 
        public fixed byte unknown[60];

        public string GetSignature()
        {
            string s;
            fixed (byte* ptr = signature_title)
            {
                byte[] bytes = new byte[480];
                int index = 0;
                for (byte* counter = ptr; *counter != 0; counter++)
                {
                    bytes[index++] = *counter;
                }
                s = Encoding.Unicode.GetString(bytes, 0, 480).TrimEnd('\0');
            }
            return s;
        }

        public string Signature => GetSignature();

        public override string ToString() => GetSignature();
    }

    public struct S5SECTHDR : ISectorHeader
    {
        public uint original_length;
        public uint length;
        public ulong OriginalLength => original_length;
        public ulong Length => length;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct S5TOCARCENTRY : ITOCARCENTRY
    {
        // There's a bunch of junk following the name which I assume is
        // uninitialized memory...
        public fixed byte filename[256]; //todo not unsigned?

        public string GetFilename()
        {
            string s;
            fixed (byte* ptr = filename)
            {
                byte[] bytes = new byte[256];
                int index = 0;
                for (byte* counter = ptr; *counter != 0; counter++)
                {
                    bytes[index++] = *counter;
                }
                s = Encoding.Unicode.GetString(bytes, 0, 256).TrimEnd('\0');
            }
            return s;
        }

        public override string ToString() => GetFilename();
    }

    //144 total?
    public struct SafeS5TOCARCENTRY
    {
        /// <remarks>132 bytes</remarks>
        public string FileName { get; set; }

        /// <remarks>4 bytes after file name //maybe archive index (which archive)</remarks>
        public uint Bytes1 { get; set; } //

        /// <summary>
        /// Location in archive (bytes offset)
        /// </summary>
        /// <remarks>4 Bytes after Bytes1</remarks>
        public uint Location { get; set; }

        /// <summary>
        /// Size of file in bytes
        /// </summary>
        /// <remarks>4 bytes after location</remarks>
        public uint FileSize { get; set; }

        public SafeS5TOCARCENTRY(byte[] buffer, int offset)
        {
            FileName = Encoding.Unicode.GetString(buffer, offset, 132).TrimEnd('\0');
            Bytes1 = BitConverter.ToUInt32(buffer, offset + 132);
            Location = BitConverter.ToUInt32(buffer, offset + 136);
            FileSize = BitConverter.ToUInt32(buffer, offset + 140);
        }

        public override string ToString() => $"{FileName}@{Bytes1}:{Location}, size: {FileSize}";
    }
}
