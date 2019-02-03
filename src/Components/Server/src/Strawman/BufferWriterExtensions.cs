using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;

namespace ConsoleApp3
{
    internal static class BufferWriterExtensions
    {
        public static void Write(this IBufferWriter<byte> writer, byte value)
        {
            var span = writer.GetSpan();
            span[0] = value;
            writer.Advance(1);
        }

        public static void Write(this IBufferWriter<byte> writer, ushort value)
        {
            var length = sizeof(ushort);
            Span<byte> span = stackalloc byte[length];
            var result = BinaryPrimitives.TryWriteUInt16BigEndian(span, value);
            Debug.Assert(result);

            writer.Write(span);
        }

        public static void Write(this IBufferWriter<byte> writer, short value)
        {
            var length = sizeof(short);
            Span<byte> span = stackalloc byte[length];
            var result = BinaryPrimitives.TryWriteInt16BigEndian(span, value);
            Debug.Assert(result);

            writer.Write(span);
        }

        public static void Write(this IBufferWriter<byte> writer, uint value)
        {
            var length = sizeof(uint);
            Span<byte> span = stackalloc byte[length];
            var result = BinaryPrimitives.TryWriteUInt32BigEndian(span, value);
            Debug.Assert(result);

            writer.Write(span);
        }

        public static void Write(this IBufferWriter<byte> writer, int value)
        {
            var length = sizeof(int);
            Span<byte> span = stackalloc byte[length];
            var result = BinaryPrimitives.TryWriteInt32BigEndian(span, value);
            Debug.Assert(result);

            writer.Write(span);
        }

        public static void Write(this IBufferWriter<byte> writer, long value)
        {
            var length = sizeof(long);
            Span<byte> span = stackalloc byte[length];
            var result = BinaryPrimitives.TryWriteInt64BigEndian(span, value);
            Debug.Assert(result);

            writer.Write(span);
        }
    }
}
