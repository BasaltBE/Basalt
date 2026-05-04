using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.RakNet.Packets
{
    public static class Magic
    {
        internal static readonly byte[] MAGIC = [0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78];
        internal const int MAGIC_LENGTH = 16;
        public static ReadOnlySpan<byte> GetBytes() => MAGIC;
        public static void Write(Span<byte> dest, int offset)
        {
            MAGIC.CopyTo(dest[offset..]);
        }
    }
}
