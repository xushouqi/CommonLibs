using ProtoBuf;

namespace CommonLibs
{
    public enum PackageCodeEnum
    {
        Register = 1001,
    }

    [ProtoContract]
    public class WebPackage
    {
        [ProtoMember(1)]
        public int ID;

        [ProtoMember(2)]
        public int ActionId;

        [ProtoMember(4)]
        public byte[] Params;

        [ProtoMember(5)]
        public byte[] Return;

        [ProtoMember(6)]
        public ErrorCodeEnum ErrorCode;

        [ProtoMember(7)]
        public int Uid;

        [ProtoMember(8)]
        public string Token;

        [ProtoMember(9)]
        public int Room = 0;

        [ProtoMember(10)]
        public int Turn = 0;

        public WebPackage ShallowCopy()
        {
            return (WebPackage)this.MemberwiseClone();
        }
    }
}
