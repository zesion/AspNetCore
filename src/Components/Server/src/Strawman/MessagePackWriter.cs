using System;
using System.Buffers;
using System.Text;

namespace ConsoleApp3
{
    public ref struct MessagePackWriter
    {
        private readonly IBufferWriter<byte> _writer;

        public MessagePackWriter(IBufferWriter<byte> writer)
        {
            _writer = writer;
        }

        public void WriteArrayLength(int length) =>
            WriteArrayLength((uint)length);

        public void WriteArrayLength(uint length)
        {
            if (length <= 0xf)
            {
                _writer.Write((byte)(0x90 | length));
            }
            else if (length <= ushort.MaxValue)
            {
                _writer.Write((byte)0xdc);
                _writer.Write((ushort)length);
            }
            else
            {
                _writer.Write((byte)0xdd);
                _writer.Write(length);
            }
        }

        public void WriteMapLength(int length) =>
            WriteMapLength((uint)length);

        public void WriteMapLength(uint length)
        {
            if (length <= 0xf)
            {
                _writer.Write((byte)(0x80 | length));
            }
            else if (length <= ushort.MaxValue)
            {
                _writer.Write((byte)0xde);
                _writer.Write((ushort)length);
            }
            else
            {
                _writer.Write((byte)0xdf);
                _writer.Write(length);
            }
        }

        public void WriteString(string value)
        {
            var length = Encoding.UTF8.GetByteCount(value);

            if (length <= 0x1f)
            {
                _writer.Write((byte)(0xa0 | length));
            }
            else if (length <= byte.MaxValue)
            {
                _writer.Write((byte)0xd9);
                _writer.Write((byte)length);
            }
            else if (length <= ushort.MaxValue)
            {
                _writer.Write((byte)0xda);
                _writer.Write((ushort)length);
            }
            else
            {
                _writer.Write((byte)0xdb);
                _writer.Write((uint)length);
            }

            var destination = _writer.GetSpan(length);

            Encoding.UTF8.GetBytes(value.AsSpan(), destination);
            _writer.Advance(length);
        }

        public void WriteUInt8(byte value)
        {
            if (value <= sbyte.MaxValue)
            {
                _writer.Write(value);
            }
            else if (value <= byte.MaxValue)
            {
                _writer.Write((byte)0xcc);
                _writer.Write(value);
            }
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

            if (value > 0)
            {
                if (value <= uint.MaxValue)
                {
                    _writer.Write((byte)0xce);
                    _writer.Write((uint)value);
                }
                else
                {
                    _writer.Write((byte)0xcf);
                    _writer.Write(value);
                }
            }
            else
            {
                _writer.Write((byte)0xcf);
                _writer.Write(value);
            }
        }

        public void WriteNil()
        {
            _writer.Write((byte)0xc0);
        }

        public void WriteBytes(byte[] bytes)
        {
            var length = bytes.Length;
            if (length <= sbyte.MaxValue)
            {
                _writer.Write((byte)0xc4);
                _writer.Write((byte)length);
            }
            else if (length <= ushort.MaxValue)
            {
                _writer.Write((byte)0xc5);
                _writer.Write((ushort)length);
            }
            else
            {
                _writer.Write((byte)0xc6);
                _writer.Write((uint)length);
            }

            _writer.Write(bytes.AsSpan());
        }


        private bool TryWriteInt32(int value)
        {
            if (value >= 0)
            {
                if (value <= sbyte.MaxValue)
                {
                    _writer.Write((byte)value);
                }
                else if (value <= byte.MaxValue)
                {
                    _writer.Write((byte)0xcc);
                    _writer.Write((byte)value);
                }
                else if (value <= ushort.MaxValue)
                {
                    _writer.Write((byte)0xcd);
                    _writer.Write((ushort)value);
                }
                else if (value <= int.MaxValue)
                {
                    _writer.Write((byte)0xce);
                    _writer.Write((uint)value);
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
                    _writer.Write((byte)value);
                }
                else if (value >= sbyte.MinValue)
                {
                    _writer.Write((byte)0xd0);
                    _writer.Write((byte)value);
                }
                else if (value >= short.MinValue)
                {
                    _writer.Write((byte)0xd1);
                    _writer.Write((short)value);
                }
                else if (value >= int.MinValue)
                {
                    _writer.Write((byte)0xd2);
                    _writer.Write(value);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
