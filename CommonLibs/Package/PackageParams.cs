using System;
using System.IO;
using System.Text;

namespace CommonLibs
{
    public class PackageParams : BaseDisposable
    {
        private const int DoubleSize = 8;
        private const int FloatSize = 4;
        private const int LongSize = 8;
        private const int IntSize = 4;
        private const int ShortSize = 2;
        private const int ByteSize = 1;
        private const int BoolSize = 1;

        private int m_position = 0;
        private Encoding m_encoding = Encoding.UTF8;
        private MemoryStream m_memoryStream;

        public PackageParams()
        {
            m_memoryStream = new MemoryStream();
        }
        public PackageParams(byte[] data)
        {
            m_position = 0;
            m_memoryStream = new MemoryStream(data, 0, data.Length);
        }
        ~PackageParams()
        {
            Dispose(false);
        }
        protected override void Dispose(bool disposing)
        {
            m_memoryStream.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// 缓冲数据大小
        /// </summary>
        public long Length
        {
            get
            {
                return m_memoryStream.Length;
            }
        }
        /// <summary>
        /// 读取的当前位置
        /// </summary>
        public long Offset
        {
            get { return m_memoryStream.Position; }
            set { m_memoryStream.Position = value; }
        }

        /// <summary>
        /// Read and remove buffer.
        /// </summary>
        /// <returns></returns>
        public byte[] PopBuffer()
        {
            var bytes = m_memoryStream.ToArray();
            return bytes;
        }

        #region ReadByte

        /// <summary>
        /// Read bool
        /// </summary>
        /// <returns></returns>
        public bool ReadBool()
        {
            byte[] bytes = ReadByte(BoolSize);
            return BitConverter.ToBoolean(bytes, 0);
        }

        /// <summary>
        /// Read float
        /// </summary>
        /// <returns></returns>
        public float ReadFloat()
        {
            byte[] bytes = ReadByte(FloatSize);
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public double ReadDouble()
        {
            byte[] bytes = ReadByte(DoubleSize);
            return BitConverter.ToDouble(bytes, 0);
        }

        /// <summary>
        /// Read short
        /// </summary>
        /// <returns></returns>
        public short ReadShort()
        {
            byte[] bytes = ReadByte(ShortSize);
            return BitConverter.ToInt16(bytes, 0);
        }
        /// <summary>
        /// Read int
        /// </summary>
        /// <returns></returns>
        public int ReadInt()
        {
            byte[] bytes = ReadByte(IntSize);
            return BitConverter.ToInt32(bytes, 0);
        }
        /// <summary>
        /// Read long
        /// </summary>
        /// <returns></returns>
        public long ReadLong()
        {
            byte[] bytes = ReadByte(LongSize);
            return BitConverter.ToInt64(bytes, 0);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public UInt16 ReadUInt16()
        {
            byte[] bytes = ReadByte(ShortSize);
            return BitConverter.ToUInt16(bytes, 0);
        }
        /// <summary>
        /// Read int
        /// </summary>
        /// <returns></returns>
        public UInt32 ReadUInt32()
        {
            byte[] bytes = ReadByte(IntSize);
            return BitConverter.ToUInt32(bytes, 0);
        }
        /// <summary>
        /// Read long
        /// </summary>
        /// <returns></returns>
        public UInt64 ReadUInt64()
        {
            byte[] bytes = ReadByte(LongSize);
            return BitConverter.ToUInt64(bytes, 0);
        }
        /// <summary>
        /// Read string
        /// </summary>
        /// <returns></returns>
        public string ReadString()
        {
            int count = ReadInt();
            byte[] bytes = ReadByte(count);
            return m_encoding.GetString(bytes);
        }

        /// <summary>
        /// Read object of Protobuf serialize.
        /// </summary>
        /// <returns></returns>
        //public object ReadObject(Type type)
        //{
        //    int count = ReadInt();
        //    byte[] bytes = ReadByte(count);
        //    return ProtoBuf.Serializer.Deserialize(bytes, type);
        //}

        /// <summary>
        /// 读取字节
        /// </summary>
        /// <returns></returns>
        public byte ReadByte()
        {
            byte[] bytes = ReadByte(ByteSize);
            return bytes[0];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ReadBytes()
        {
            int len = ReadInt();
            return ReadByte(len);
        }

        /// <summary>
        /// 读取字节
        /// </summary>
        /// <param name="count">读取的个数</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public byte[] ReadByte(int count)
        {
            VerifyBufferLength(count);
            byte[] bytes = new byte[count];
            int len = m_memoryStream.Read(bytes, 0, count);
            m_position += len;
            return bytes;
        }

        private void VerifyBufferLength(int count)
        {
            long len = Length - Offset;
            if (count < 0 || count > len)
            {
                throw new ArgumentOutOfRangeException(string.Format("Read {0} byte len overflow max {1} len.", count, len));
            }
        }

        /// <summary>
        /// 获取缓冲区字节数据，不移除
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public byte[] PeekByte(int count)
        {
            VerifyBufferLength(count);
            byte[] bytes = new byte[count];
            int len = m_memoryStream.Read(bytes, 0, count);
            m_memoryStream.Position = m_memoryStream.Position - len;

            //int index = 0;
            //foreach (var buffer in _buffersQueue)
            //{
            //    data[index] = buffer;
            //    index++;
            //    if (index >= count)
            //    {
            //        break;
            //    }
            //}
            return bytes;
        }

        /// <summary>
        /// To Hex(16) string
        /// </summary>
        /// <returns></returns>
        public string ToHexString()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                var buffer = m_memoryStream.ToArray();
                if (buffer != null)
                {
                    int colIndex = 0;
                    int index = 0;
                    foreach (var b in buffer)
                    {
                        sb.AppendFormat(" {0}", b.ToString("X2"));
                        index++;
                        if (index % 8 == 0)
                        {
                            sb.Append(" ");
                            colIndex++;
                        }
                        if (colIndex == 2)
                        {
                            sb.AppendLine();
                            colIndex = 0;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return sb.ToString();
        }
        /// <summary>
        /// 检查是否有压缩数据
        /// </summary>
        /// <returns></returns>
        private bool CheckGzipBuffer()
        {
            byte[] gzipHead = PeekByte(IntSize);
            return CheckEnableGzip(gzipHead);
        }

        /// <summary>
        /// 检查是否有Gzip压缩
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool CheckEnableGzip(byte[] data)
        {
            if (data != null && data.Length > 3)
            {
                return data[0] == 0x1f && data[1] == 0x8b && data[2] == 0x08 && data[3] == 0x00;
            }
            return false;
        }

        #endregion ReadByte end

        #region WriteByte
        /// <summary>
        /// Write bool
        /// </summary>
        /// <param name="value"></param>
        public void WriteByte(bool value)
        {
            byte[] bytes = BitConverter.GetBytes(value);//1
            WriteByte(bytes);
        }
        /// <summary>
        /// Write float
        /// </summary>
        /// <param name="value"></param>
        public void WriteByte(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);//4
            WriteByte(bytes);
        }
        /// <summary>
        /// Write byte
        /// </summary>
        /// <param name="value"></param>
        public void WriteByte(double value)
        {
            byte[] bytes = BitConverter.GetBytes(value);//8
            WriteByte(bytes);
        }
        /// <summary>
        /// Write long
        /// </summary>
        /// <param name="value"></param>
        public void WriteByte(long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            WriteByte(bytes);
        }

        /// <summary>
        /// Write Int
        /// </summary>
        /// <param name="value"></param>
        public void WriteByte(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);//4
            WriteByte(bytes);
        }
        /// <summary>
        /// Write short
        /// </summary>
        /// <param name="value"></param>
        public void WriteByte(short value)
        {
            byte[] bytes = BitConverter.GetBytes(value);//2
            WriteByte(bytes);
        }
        /// <summary>
        /// Write long
        /// </summary>
        /// <param name="value"></param>
        public void WriteByte(UInt64 value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            WriteByte(bytes);
        }

        /// <summary>
        /// Write Int
        /// </summary>
        /// <param name="value"></param>
        public void WriteByte(UInt32 value)
        {
            byte[] bytes = BitConverter.GetBytes(value);//4
            WriteByte(bytes);
        }
        /// <summary>
        /// Write short
        /// </summary>
        /// <param name="value"></param>
        public void WriteByte(UInt16 value)
        {
            byte[] bytes = BitConverter.GetBytes(value);//2
            WriteByte(bytes);
        }

        /// <summary>
        /// Write string
        /// </summary>
        /// <param name="value"></param>
        public void WriteByte(string value)
        {
            value = value ?? "";
            byte[] bytes = m_encoding.GetBytes(value);
            byte[] lengthBytes = BitConverter.GetBytes(bytes.Length);
            WriteByte(lengthBytes);
            if (bytes.Length > 0)
            {
                WriteByte(bytes);
            }
        }

        /// <summary>
        /// Write object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="useGzip"></param>
        //public void WriteByte(object obj, bool useGzip = true)
        //{
        //    try
        //    {
        //        var bytes = ProtoBufUtils.Serialize(obj, useGzip);
        //        byte[] lengthBytes = BitConverter.GetBytes(bytes.Length);
        //        WriteByte(lengthBytes);
        //        if (bytes.Length > 0)
        //        {
        //            WriteByte(bytes);
        //        }
        //    }
        //    catch (Exception err)
        //    {
        //        throw new ArgumentOutOfRangeException("obj is not a valid data can be serialized.", err);
        //    }
        //}

        /// <summary>
        /// Write byte
        /// </summary>
        /// <param name="value"></param>
        public void WriteByte(byte value)
        {
            byte[] bytes = new byte[] { value };
            WriteByte(bytes);
        }

        /// <summary>
        /// Write bytes
        /// </summary>
        /// <param name="buffer"></param>
        public void WriteByte(byte[] buffer)
        {
            WriteByte(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 写入字节
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void WriteByte(byte[] buffer, int offset, int count)
        {
            if (buffer.Length < count)
            {
                throw new ArgumentOutOfRangeException("count", "buffer size outof range");
            }
            m_memoryStream.Write(buffer, offset, count);
            //for (int i = offset; i < count; i++)
            //{
            //    _buffersQueue.Enqueue(buffer[i]);
            //}
        }

        /// <summary>
        /// 占用字节大小
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private int GetByteSize(object item)
        {
            int length = 0;
            if (item is string)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(item.ToString());
                length += bytes.Length + IntSize;
            }
            else if (item is long)
            {
                length += LongSize;
            }
            else if (item is int)
            {
                length += IntSize;
            }
            else if (item is short)
            {
                length += ShortSize;
            }
            else if (item is byte)
            {
                length += ByteSize;
            }
            else if (item is bool)
            {
                length += BoolSize;
            }
            else if (item is double)
            {
                length += DoubleSize;
            }
            else if (item is float)
            {
                length += FloatSize;
            }
            else if (item is DateTime)
            {
                length += LongSize;
            }
            //else if (item is MessageStructure)
            //{
            //    length += (item as MessageStructure).GetStackByteSize();
            //}
            else if (item is byte[])
            {
                length += (item as byte[]).Length + 4;
            }
            return length;
        }
        #endregion Writebyte
    }
}
