using System;
using ProtoBuf;

namespace CommonLibs
{
    public class ProtoBufUtils
    {
        public static byte[] Serialize(object obj)
        {
            byte[] data = null;
            if (obj != null)
            {
                using (var ms = new System.IO.MemoryStream())
                {
                    ProtoBuf.Serializer.Serialize(ms, obj);
                    data = ms.ToArray();
                }
            }
            return data;
        }
        public static T Deserialize<T>(byte[] data)
        {
            T inst = default(T);
            if (data != null && data.Length > 0)
            {
                using (var ms = new System.IO.MemoryStream(data))
                {
                    inst = ProtoBuf.Serializer.Deserialize<T>(ms);
                }
            }            
            return inst;
        }
        public static T Deserialize<T>(byte[] data, int index, int count)
        {
            T inst = default(T);
            using (var ms = new System.IO.MemoryStream(data, index, count))
            {
                inst = ProtoBuf.Serializer.Deserialize<T>(ms);
            }
            return inst;
        }
    }
}
