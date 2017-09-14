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
        public int ErrorCode;

        [ProtoMember(7)]
        public int Uid;

        [ProtoMember(8)]
        public string Token;

        public ErrorCodeEnum MyError { get { return (ErrorCodeEnum)ErrorCode; } }
    }
}
