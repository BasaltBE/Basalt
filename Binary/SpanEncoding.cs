using System.Buffers.Binary;

namespace Basalt.Binary
{
    public static partial class SpanEncodingExtensions {
        extension(Span<byte> source)
        {
            public void WriteInt32(int value, int offset = 0, bool littleEndian = true)
            {
                if (littleEndian) BinaryPrimitives.WriteInt32LittleEndian(source[offset..], value);
                else BinaryPrimitives.WriteInt32BigEndian(source[offset..], value);
            }
        }
        extension(ReadOnlySpan<byte> source)
        {

        }
    }
}
