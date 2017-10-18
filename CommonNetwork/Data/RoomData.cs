using System;
using ProtoBuf;

namespace CommonNetwork
{
    [ProtoContract]
    public class RoomData
    {
        [ProtoMember(1)]
        public int ID { get; set; }


        [ProtoMember(10)]
        public RoomPlayerData[] Players { get; set; }
    }
}
