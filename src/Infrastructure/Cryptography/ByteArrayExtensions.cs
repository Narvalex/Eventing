namespace Infrastructure.Cryptography
{
    // Source: http://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa/24343727#24343727
    public static class ByteArrayExtensions
    {
        public static string ToHexString(this byte[] bytes)
        {
            var lookup32 = _lookup32;
            var result = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                var val = lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }      

        private static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("X2");
                result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
            }
            return result;
        }

        private static readonly uint[] _lookup32 = CreateLookup32();
        
        // UNSAFE VERSION
        //public unsafe static string ToHexString(this byte[] byteArray)
        //{
        //    var lookupPointer = _lookup32UnsafePointer;
        //    var result = new char[byteArray.Length * 2];
        //    fixed (byte* bytesPointer = byteArray)
        //    fixed (char* resultPointer = result)
        //    {
        //        uint* resultPointer2 = (uint*)resultPointer;
        //        for (int i = 0; i < byteArray.Length; i++)
        //            resultPointer2[i] = lookupPointer[bytesPointer[i]];
        //    }
        //    return new string(result);
        //}

        //private static readonly uint[] _lookup32Unsafe = CreateLookup32Unsafe();
        //private unsafe static readonly uint* _lookup32UnsafePointer =
        //    (uint*)GCHandle
        //    .Alloc(_lookup32Unsafe, GCHandleType.Pinned)
        //    .AddrOfPinnedObject();

        //private static uint[] CreateLookup32Unsafe()
        //{
        //    var result = new uint[256];
        //    for (int i = 0; i < 256; i++)
        //    {
        //        var s = i.ToString("x2");
        //        if (BitConverter.IsLittleEndian)
        //            result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
        //        else
        //            result[i] = ((uint)s[1]) + ((uint)s[0] << 16);
        //    }

        //    return result;
        //}
    }
}
