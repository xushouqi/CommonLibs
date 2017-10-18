using ProtoBuf;

namespace GodModels.ViewModels
{
    [ProtoContract]
    public class MatchData
    {
        [ProtoMember(1)]
        public int Key { get; set; }
        [ProtoMember(2)]
        public MatchTypeEnum Type { get; set; }
        [ProtoMember(3)]
        public MatchStateEnum State { get; set; }
        /// <summary>
        /// 所属联赛/杯赛的ID
        /// </summary>
        [ProtoMember(4)]
        public int HostId { get; set; }
        /// <summary>
        /// 轮次
        /// </summary>
        [ProtoMember(5)]
        public int Round { get; set; }
        [ProtoMember(6)]
        public int Team1 { get; set; }
        [ProtoMember(7)]
        public int Team2 { get; set; }
        [ProtoMember(8)]
        public int Winner { get; set; }
        [ProtoMember(9)]
        public float CurTime { get; set; }

    }
}
