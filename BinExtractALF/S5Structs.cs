using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BinExtractALF
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct S5HDR : IHeader
    {
        public fixed byte signature_title[480]; // "S4IC413 <title>", "S4AC422 <title>" //todo not unsigned?
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
        public string FileName { get; set; } //unknown number of bytes 132?
        public uint Bytes1 { get; set; } //4 bytes after file name //maybe archive index (which archive)
        public uint Bytes2 { get; set; } //4 bytes after Bytes2 //maybe location in archive 
        public uint FileSize { get; set; } //4 bytes after Bytes2

        public SafeS5TOCARCENTRY(byte[] buffer, int offset)
        {
            FileName = Encoding.Unicode.GetString(buffer, offset, 132).TrimEnd('\0');
            Bytes1 = BitConverter.ToUInt32(buffer, offset + 132);
            Bytes2 = BitConverter.ToUInt32(buffer, offset + 136);
            FileSize = BitConverter.ToUInt32(buffer, offset + 140);
        }

        public override string ToString() => $"{FileName}@{Bytes1}:{Bytes2}, size: {FileSize}";
    }
}
