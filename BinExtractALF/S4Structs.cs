using System.Runtime.InteropServices;
using System.Text;
using AGF2BMP2AGF;

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
    
    public struct S4SECTHDR : ISectorHeader
    {
        public uint original_length;
        // ReSharper disable once NotAccessedField.Global
        public uint original_length2; // why?
        public uint length;
        public ulong OriginalLength  => original_length;
        public ulong Length => length;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct S4TOCARCHDR
    {
        public uint entry_count;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct S4TOCARCENTRY : ITOCARCENTRY
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
                s = Encoding.UTF8.GetString(bytes, 0, 256).TrimEnd('\0');
            }
            return s;
        }

        public override string ToString() => GetFilename();
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct S4FileEntry : IFileEntry
    {
        public fixed byte filename[64]; //todo not unsigned?
        public uint archive_index;
        public uint file_index; //within archive?
        public uint offset;
        public uint length;

        public string GetFilename()
        {
            string s;
            fixed (byte* ptr = filename)
            {
                byte[] bytes = new byte[64];
                int index = 0;
                for (byte* counter = ptr; *counter != 0; counter++)
                {
                    bytes[index++] = *counter;
                }
                s = Encoding.UTF8.GetString(bytes, 0, 64).TrimEnd('\0');
            }
            return s;
        }

        public string FileName => GetFilename();
        public uint ArchiveIndex => archive_index;
        public uint FileIndex => file_index;
        public uint Offset => offset;
        public uint Length => length;
    };
    

}
