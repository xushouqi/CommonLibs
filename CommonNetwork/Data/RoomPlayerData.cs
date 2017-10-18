using System;
using ProtoBuf;

namespace CommonNetwork
{
    [ProtoContract]
    public class RoomPlayerData
    {
        [ProtoMember(1)]
        public int Uid { get; set; }
    }
}
