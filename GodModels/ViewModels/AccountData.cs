using ProtoBuf;

namespace GodModels.ViewModels
{
    [ProtoContract]
    public class AccountData
    {
        [ProtoMember(1)]
        public string UserName { get; set; }
        [ProtoMember(2)]
        public CommonLibs.UserTypeEnum Type { get; set; }
        [ProtoMember(3)]
        public AccountStateEnum State { get; set; }
        [ProtoMember(4)]
        public string Name { get; set; }
        [ProtoMember(5)]
        public RoleData MyRole { get; set; }
        [ProtoMember(6)]
        public string Token { get; set; }
        [ProtoMember(7)]
        public int ExpiresIn { get; set; }
        [ProtoMember(8)]
        public int Key { get; set; }

    }
}
