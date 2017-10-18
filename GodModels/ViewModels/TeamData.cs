using ProtoBuf;

namespace GodModels.ViewModels
{
    [ProtoContract]
    public class TeamData
    {
        [ProtoMember(1)]
        public int Key { get; set; }
        [ProtoMember(2)]
        public int ID { get; set; }
        [ProtoMember(3)]
        public int RoleId { get; set; }
        [ProtoMember(4)]
        public string Name { get; set; }
        [ProtoMember(5)]
        public TeamTypeEnum Type { get; set; }
        /// <summary>
        /// 赛区
        /// </summary>
        [ProtoMember(6)]
        public int Region { get; set; }
        /// <summary>
        /// 等级
        /// </summary>
        [ProtoMember(7)]
        public int Grade { get; set; }
        /// <summary>
        /// 粉丝数（用于升级）
        /// </summary>
        [ProtoMember(8)]
        public int Fans { get; set; }
        /// <summary>
        /// 总生涯天数(从周一开始)
        /// </summary>
        [ProtoMember(9)]
        public int CareerDays { get; set; }
        /// <summary>
        /// 金钱
        /// </summary>
        [ProtoMember(10)]
        public int Money { get; set; }
        /// <summary>
        /// 积分（来自于PVP）
        /// </summary>
        [ProtoMember(11)]
        public int Point { get; set; }

    }
}
