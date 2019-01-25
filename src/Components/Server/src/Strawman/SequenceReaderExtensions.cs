using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConsoleApp3
{
    public static class SequenceReaderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out ushort value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return TryRead(ref reader, out value);
            }

            return TryReadReverseEndianness(ref reader, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out uint value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return TryRead(ref reader, out value);
            }

            return TryReadReverseEndianness(ref reader, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out ulong value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return TryRead(ref reader, out value);
            }

            return TryReadReverseEndianness(ref reader, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryReadReverseEndianness(ref SequenceReader<byte> reader, out ushort value)
        {
            if (TryRead(ref reader, out value))
            {
                value = BinaryPrimitives.ReverseEndianness(value);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryReadReverseEndianness(ref SequenceReader<byte> reader, out uint value)
        {
            if (TryRead(ref reader, out value))
            {
                value = BinaryPrimitives.ReverseEndianness(value);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryReadReverseEndianness(ref SequenceReader<byte> reader, out ulong value)
        {
            if (TryRead(ref reader, out value))
            {
                value = BinaryPrimitives.ReverseEndianness(value);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool TryRead<T>(ref SequenceReader<byte> reader, out T value) where T : unmanaged
        {
            var span = reader.UnreadSpan;
            if (span.Length < sizeof(T))
            {
                return TryReadMultisegment(ref reader, out value);
            }

            value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(span));
            reader.Advance(sizeof(T));
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool TryReadMultisegment<T>(ref SequenceReader<byte> reader, out T value) where T : unmanaged
        {
            Debug.Assert(reader.UnreadSpan.Length < sizeof(T));

            // Not enough data in the current segment, try to peek for the data we need.
            T buffer = default;
            var tempSpan = new Span<byte>(&buffer, sizeof(T));

            if (!reader.TryCopyTo(tempSpan))
            {
                value = default;
                return false;
            }

            value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(tempSpan));
            reader.Advance(sizeof(T));
            return true;
        }
    }
}
