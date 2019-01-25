using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace ConsoleApp3
{
    public ref struct MessagePackReader
    {
        private readonly ReadOnlySequence<byte> _sequence;
        private SequenceReader<byte> _reader;

        public MessagePackReader(ref ReadOnlySequence<byte> sequence)
        {
            _sequence = sequence;
            _reader = new SequenceReader<byte>(_sequence);
        }

        /// <summary>
        /// Advances the buffer if a nil is read.
        /// </summary>
        public bool TryReadNil()
        {
            return _reader.TryPeek(out var format)
                && format == 0xc0
                && _reader.TryRead(out _);
        }

        public bool TryReadArrayLength(out uint value)
        {
            if (!_reader.TryRead(out var format))
            {
                value = default;
                return false;
            }

            switch (format)
            {
                case var _ when format >= 0x90 && format <= 0x9f:
                    value = (uint)(format & 0xF);
                    return true;

                case 0xdc when _reader.TryReadBigEndian(out ushort shortValue):
                    value = shortValue;
                    return true;

                case 0xdd when _reader.TryReadBigEndian(out uint intValue):
                    value = intValue;
                    return true;

                default:
                    value = default;
                    return false;
            }
        }

        public bool TryReadMapLength(out uint value)
        {
            if (!_reader.TryRead(out var format))
            {
                value = default;
                return false;
            }

            switch (format)
            {
                case var _ when format >= 0x80 && format <= 0x8f:
                    value = (uint)(format & 0xF);
                    return true;

                case 0xde when _reader.TryReadBigEndian(out ushort shortValue):
                    value = shortValue;
                    return true;

                case 0xdf when _reader.TryReadBigEndian(out uint intValue):
                    value = intValue;
                    return true;

                default:
                    value = default;
                    return false;
            }
        }

        public bool TryReadString(out string value)
        {
            if (!_reader.TryRead(out var format))
            {
                value = default;
                return false;
            }

            uint length;
            switch (format)
            {
                case var _ when format >= 0xa0 && format <= 0xbf:
                    length = Convert.ToUInt32(format & 0x1F);
                    break;

                case 0xd9:
                    if (!_reader.TryRead(out var byteLength))
                    {
                        value = default;
                        return false;
                    }

                    length = byteLength;
                    break;

                case 0xda:
                    if (!_reader.TryReadBigEndian(out ushort shortValue))
                    {
                        value = default;
                        return false;
                    }

                    length = shortValue;
                    break;

                case 0xdb:
                    if (!_reader.TryReadBigEndian(out length))
                    {
                        value = default;
                        return false;
                    }

                    break;

                default:
                    value = default;
                    return false;
            }

            var slice = _reader.Sequence.Slice(_reader.Position, length);
            _reader.Advance(length);
            if (slice.IsSingleSegment)
            {
                value = Encoding.UTF8.GetString(slice.First.Span);
            }
            else
            {
                value = string.Create((int)slice.Length, slice, (span, sequence) =>
                {
                    foreach (var segment in sequence)
                    {
                        Encoding.UTF8.GetChars(segment.Span, span);

                        span = span.Slice(segment.Length);
                    }
                });
            }

            return true;
        }

        public bool TryReadFixNum(out byte value)
        {
            value = default;

            return _reader.TryRead(out var format) &&
                TryReadFixNum(ref _reader, format, out value);
        }

        public bool TryReadUInt8(out byte value)
        {
            value = default;

            return _reader.TryRead(out var format) &&
                TryReadUInt8(ref _reader, format, out value);
        }

        public bool TryReadUInt16(out ushort value)
        {
            value = default;

            return _reader.TryRead(out var format) &&
                TryReadUInt16(ref _reader, format, out value);
        }

        public bool TryReadUInt32(out uint value)
        {
            value = default;

            return _reader.TryRead(out var format) &&
                TryReadUInt32(ref _reader, format, out value);
        }

        public bool TryReadUInt64(out ulong value)
        {
            value = default;

            return _reader.TryRead(out var format) &&
                TryReadUInt64(ref _reader, format, out value);
        }

        public bool TryReadFixNum(out sbyte value)
        {
            value = default;

            return _reader.TryRead(out var format) &&
                TryReadFixNum(ref _reader, format, out value);
        }

        public bool TryReadInt8(out sbyte value)
        {
            value = default;

            return _reader.TryRead(out var format) &&
                TryReadInt8(ref _reader, format, out value);
        }

        public bool TryReadInt16(out short value)
        {
            value = default;

            return _reader.TryRead(out var format) &&
                TryReadInt16(ref _reader, format, out value);
        }

        public bool TryReadInt32(out int value)
        {
            value = default;

            return _reader.TryRead(out var format) &&
                TryReadInt32(ref _reader, format, out value);
        }

        public bool TryReadInt64(out long value)
        {
            value = default;

            return _reader.TryRead(out var format) &&
                TryReadInt64(ref _reader, format, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryReadFixNum(ref SequenceReader<byte> _, byte format, out byte value)
        {
            if (format >= 0 && format <= 0x80)
            {
                value = format;
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryReadFixNum(ref SequenceReader<byte> _, byte format, out sbyte value)
        {
            if (format >= 0xe0 && format <= 0xff)
            {
                value = (sbyte)format;
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryReadUInt8(ref SequenceReader<byte> reader, byte format, out byte value)
        {
            if (format == 0xcc)
            {
                if (reader.TryRead(out value))
                {
                    return true;
                }
            }
            else if (TryReadFixNum(ref reader, format, out value))
            {
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryReadInt8(ref SequenceReader<byte> reader, byte format, out sbyte value)
        {
            if (format == 0xd0)
            {
                if (reader.TryRead(out var byteValue))
                {
                    value = (sbyte)byteValue;
                    return true;
                }
            }
            else if (TryReadFixNum(ref reader, format, out value))
            {
                return true;
            }
            else if (TryReadFixNum(ref reader, format, out byte byteValue))
            {
                value = checked((sbyte)byteValue);
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryReadUInt16(ref SequenceReader<byte> reader, byte format, out ushort value)
        {
            if (format == 0xcd)
            {
                if (reader.TryReadBigEndian(out value))
                {
                    return true;
                }
            }
            else if (TryReadUInt8(ref reader, format, out var byteValue))
            {
                value = byteValue;
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryReadInt16(ref SequenceReader<byte> reader, byte format, out short value)
        {
            if (format == 0xd1)
            {
                if (reader.TryReadBigEndian(out value))
                {
                    return true;
                }
            }
            else if (TryReadUInt16(ref reader, format, out var ushortValue))
            {
                value = Convert.ToInt16(ushortValue);
                return true;
            }
            else if (TryReadInt8(ref reader, format, out var sbyteValue))
            {
                value = sbyteValue;
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryReadUInt32(ref SequenceReader<byte> reader, byte format, out uint value)
        {
            if (format == 0xce)
            {
                if (reader.TryReadBigEndian(out value))
                {
                    return true;
                }
            }
            else if (TryReadUInt16(ref reader, format, out var shortValue))
            {
                value = shortValue;
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryReadInt32(ref SequenceReader<byte> reader, byte format, out int value)
        {
            if (format == 0xd2)
            {
                if (reader.TryReadBigEndian(out value))
                {
                    return true;
                }
            }
            else if (TryReadUInt32(ref reader, format, out var uintValue))
            {
                value = Convert.ToInt32(uintValue);
                return true;
            }
            else if (TryReadInt16(ref reader, format, out var shortValue))
            {
                value = shortValue;
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryReadUInt64(ref SequenceReader<byte> reader, byte format, out ulong value)
        {
            if (format == 0xcf)
            {
                if (reader.TryReadBigEndian(out value))
                {
                    return true;
                }
            }
            else if (TryReadUInt32(ref reader, format, out var intValue))
            {
                value = intValue;
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryReadInt64(ref SequenceReader<byte> reader, byte format, out long value)
        {
            if (format == 0xd3)
            {
                if (reader.TryReadBigEndian(out value))
                {
                    return true;
                }
            }
            else if (TryReadUInt64(ref reader, format, out var ulongValue))
            {
                value = Convert.ToInt64(ulongValue);
                return true;
            }
            else if (TryReadInt32(ref reader, format, out var intValue))
            {
                value = intValue;
                return true;
            }

            value = default;
            return false;
        }
    }
}
