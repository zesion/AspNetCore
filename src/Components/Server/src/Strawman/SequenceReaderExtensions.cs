using System.Buffers;
using System.Runtime.CompilerServices;

namespace ConsoleApp3
{
    public static class SequenceReaderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out ushort value)
        {
            if (reader.TryReadBigEndian(out short shortValue))
            {
                value = unchecked((ushort)shortValue);
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out uint value)
        {
            if (reader.TryReadBigEndian(out int intValue))
            {
                value = unchecked((uint)intValue);
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out ulong value)
        {
            if (reader.TryReadBigEndian(out long longValue))
            {
                value = unchecked((ulong)longValue);
                return true;
            }

            value = default;
            return false;
        }
    }
}
