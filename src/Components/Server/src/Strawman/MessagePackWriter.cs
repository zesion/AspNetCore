using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace ConsoleApp3
{
    public ref struct MessagePackWriter
    {
        private const int DefaultGrowthSize = 1024;
        private readonly IBufferWriter<byte> _writer;
        private Span<byte> _buffer;
        private int _buffered;

        public MessagePackWriter(IBufferWriter<byte> writer)
        {
            _writer = writer;
            _buffer = writer.GetSpan();
            BytesCommitted = 0;
            _buffered = 0;
        }

        public long BytesCommitted { get; private set; }

        public void WriteArrayLength(int length) =>
            WriteArrayLength((uint)length);

        public void WriteArrayLength(uint length)
        {
            var index = 0;
            if (length <= 0xf)
            {
                Write((byte)(0x90 | length), ref index);
            }
            else if (length <= ushort.MaxValue)
            {
                Write((byte)0xdc, ref index);
                Write((ushort)length, ref index);
            }
            else
            {
                Write((byte)0xdd, ref index);
                Write(length, ref index);
            }

            Advance(index);
        }

        public void WriteMapLength(int length) => WriteMapLength((uint)length);

        public void WriteMapLength(uint length)
        {
            var index = 0;
            if (length <= 0xf)
            {
                Write((byte)(0x80 | length), ref index);
            }
            else if (length <= ushort.MaxValue)
            {
                Write((byte)0xde, ref index);
                Write((ushort)length, ref index);
            }
            else
            {
                Write((byte)0xdf, ref index);
                Write(length, ref index);
            }

            Advance(index);
        }

        public void WriteString(string value)
        {
            var length = Encoding.UTF8.GetByteCount(value);

            var index = 0;
            if (length <= 0x1f)
            {
                Write((byte)(0xa0 | length), ref index);
            }
            else if (length <= byte.MaxValue)
            {
                Write((byte)0xd9, ref index);
                Write((byte)length, ref index);
            }
            else if (length <= ushort.MaxValue)
            {
                Write((byte)0xda, ref index);
                Write((ushort)length, ref index);
            }
            else
            {
                Write((byte)0xdb, ref index);
                Write((uint)length, ref index);
            }

            if (_buffer.Length - index < length)
            {
                AdvanceAndGrow(ref index, length);
            }

            index += Encoding.UTF8.GetBytes(value.AsSpan(), _buffer.Slice(index));
            Advance(index);
        }

        public void WriteUInt8(byte value)
        {
            var index = 0;

            if (value <= sbyte.MaxValue)
            {
                Write(value, ref index);
            }
            else if (value <= byte.MaxValue)
            {
                Write((byte)0xcc, ref index);
                Write(value, ref index);
            }

            Advance(index);
        }

        public void WriteInt32(int value)
        {
            TryWriteInt32(value);
        }

        public void WriteInt64(long value)
        {
            if (value >= int.MinValue && value <= int.MaxValue && TryWriteInt32((int)value))
            {
                return;
            }
            var index = 0;

            if (value > 0)
            {
                if (value <= uint.MaxValue)
                {
                    Write((byte)0xce, ref index);
                    Write((uint)value, ref index);
                }
                else
                {
                    Write((byte)0xcf, ref index);
                    Write(value, ref index);
                }
            }
            else
            {
                Write((byte)0xcf, ref index);
                Write(value, ref index);
            }

            Advance(index);
        }

        public void WriteNil()
        {
            var index = 0;
            Write((byte)0xc0, ref index);
            Advance(index);
        }

        public void WriteBytes(byte[] bytes)
        {
            var index = 0;

            var length = bytes.Length;
            if (length <= sbyte.MaxValue)
            {
                Write((byte)0xc4, ref index);
                Write((byte)length, ref index);
            }
            else if (length <= ushort.MaxValue)
            {
                Write((byte)0xc5, ref index);
                Write((ushort)length, ref index);
            }
            else
            {
                Write((byte)0xc6, ref index);
                Write((uint)length, ref index);
            }

            if (_buffer.Length - index < length)
            {
                AdvanceAndGrow(ref index, length);
            }

            bytes.AsSpan().CopyTo(_buffer.Slice(index));
            index += length;
            Advance(index);
        }


        private void Write(byte value, ref int index)
        {
            if (_buffer.IsEmpty)
            {
                AdvanceAndGrow(ref index);
            }

            _buffer[index] = value;
            index++;
        }

        private void Write(ushort value, ref int index)
        {
            if (!BinaryPrimitives.TryWriteUInt16BigEndian(_buffer.Slice(index), value))
            {
                AdvanceAndGrow(ref index);
                var result = BinaryPrimitives.TryWriteUInt16BigEndian(_buffer, value);
                Debug.Assert(result);
            }
            index += 2;
        }

        private void Write(short value, ref int index)
        {
            if (!BinaryPrimitives.TryWriteInt16BigEndian(_buffer.Slice(index), value))
            {
                AdvanceAndGrow(ref index);
                var result = BinaryPrimitives.TryWriteInt16BigEndian(_buffer, value);
                Debug.Assert(result);
            }
            index += 2;
        }

        private void Write(uint value, ref int index)
        {
            if (!BinaryPrimitives.TryWriteUInt32BigEndian(_buffer.Slice(index), value))
            {
                AdvanceAndGrow(ref index);
                var result = BinaryPrimitives.TryWriteUInt32BigEndian(_buffer, value);
                Debug.Assert(result);
            }
            index += 4;
        }

        private void Write(int value, ref int index)
        {
            if (!BinaryPrimitives.TryWriteInt32BigEndian(_buffer.Slice(index), value))
            {
                AdvanceAndGrow(ref index);
                var result = BinaryPrimitives.TryWriteInt32BigEndian(_buffer, value);
                Debug.Assert(result);
            }
            index += 4;
        }

        private void Write(long value, ref int index)
        {
            if (!BinaryPrimitives.TryWriteInt64BigEndian(_buffer.Slice(index), value))
            {
                AdvanceAndGrow(ref index);
                var result = BinaryPrimitives.TryWriteInt64BigEndian(_buffer, value);
                Debug.Assert(result);
            }
            index += 8;
        }

        private bool TryWriteInt32(int value)
        {
            var index = 0;
            if (value >= 0)
            {
                if (value <= sbyte.MaxValue)
                {
                    Write((byte)value, ref index);
                }
                else if (value <= byte.MaxValue)
                {
                    Write((byte)0xcc, ref index);
                    Write((byte)value, ref index);
                }
                else if (value <= ushort.MaxValue)
                {
                    Write((byte)0xcd, ref index);
                    Write((ushort)value, ref index);
                }
                else if (value <= int.MaxValue)
                {
                    Write((byte)0xce, ref index);
                    Write((uint)value, ref index);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (value >= -32)
                {
                    Write((byte)value, ref index);
                }
                else if (value >= sbyte.MinValue)
                {
                    Write((byte)0xd0, ref index);
                    Write((byte)value, ref index);
                }
                else if (value >= short.MinValue)
                {
                    Write((byte)0xd1, ref index);
                    Write((short)value, ref index);
                }
                else if (value >= int.MinValue)
                {
                    Write((byte)0xd2, ref index);
                    Write(value, ref index);
                }
                else
                {
                    return false;
                }
            }

            Advance(index);
            return true;
        }

        private void GrowAndEnsure(int growSize)
        {
            Flush();
            var previousSpanLength = _buffer.Length;
            Debug.Assert(previousSpanLength < growSize);
            _buffer = _writer.GetSpan(growSize);
            if (_buffer.Length <= previousSpanLength)
            {
                ThrowFailedToGetMinimumSizeSpan();
            }
        }

        private static void ThrowFailedToGetMinimumSizeSpan()
        {
            throw new InvalidOperationException($"Unable to grow the span by {DefaultGrowthSize}.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AdvanceAndGrow(ref int alreadyWritten, int growSize = DefaultGrowthSize)
        {
            Debug.Assert(alreadyWritten >= 0);
            Advance(alreadyWritten);
            GrowAndEnsure(growSize);
            alreadyWritten = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Advance(int count)
        {
            Debug.Assert(count >= 0 && _buffered <= int.MaxValue - count);

            _buffered += count;
            _buffer = _buffer.Slice(count);
        }

        public void Flush()
        {
            _writer.Advance(_buffered);
            BytesCommitted += _buffered;
            _buffered = 0;
        }
    }
}
