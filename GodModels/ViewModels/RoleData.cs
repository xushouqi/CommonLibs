using ProtoBuf;

namespace GodModels.ViewModels
{
    [ProtoContract]
    public class RoleData
    {
        [ProtoMember(1)]
        public int Key { get; set; }
        [ProtoMember(2)]
        public int AccountId { get; set; }
        [ProtoMember(3)]
        public string Name { get; set; }
        /// <summary>
        /// 钻石
        /// </summary>
        [ProtoMember(4)]
        public int Diamond { get; set; }
        /// <summary>
        /// 体力
        /// </summary>
        [ProtoMember(5)]
        public int Power { get; set; }
        [ProtoMember(6)]
        public TeamData MyTeam { get; set; }

    }
}
