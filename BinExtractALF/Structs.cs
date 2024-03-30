using System.Runtime.InteropServices;
using System.Text;

namespace BinExtractALF
{

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct S4HDR
    {
        public fixed byte signature_title[240]; // "S4IC413 <title>", "S4AC422 <title>" //todo not unsigned?
        public fixed byte unknown[60];

        public string GetSignature()
        {
            string s;
            fixed (byte* ptr = signature_title)
            {
                byte[] bytes = new byte[240];
                int index = 0;
                for (byte* counter = ptr; *counter != 0; counter++)
                {
                    bytes[index++] = *counter;
                }
                s = Encoding.Unicode.GetString(bytes, 0, 240).TrimEnd('\0');
            }
            return s;
        }

        public override string ToString() => GetSignature();
    }



    interface ISectorHeader
    {
        ulong OriginalLength { get; }
        ulong Length { get; }
    }



    public struct S5SECTHDR : ISectorHeader
    {
        public uint original_length;
        public uint length;
        public ulong OriginalLength => original_length;
        public ulong Length => length;
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
    struct S4TOCARCHDR
    {
        public uint entry_count;
    }

    public interface ITOCARCENTRY
    {
        string GetFilename();
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
    
    public struct SafeTOCARCENTRY
    {
        public string FileName { get; set; }
        public SafeTOCARCENTRY(ITOCARCENTRY data)
        {
            FileName = data.GetFilename();
        }

        public override string ToString() => FileName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct S4TOCFILENTRY
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
    };


    [StructLayout(LayoutKind.Sequential)]
    public struct arc_info_t
    {
        public string? fd;
        public string dir;
    };

}
