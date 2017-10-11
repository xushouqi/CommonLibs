using ProtoBuf;

namespace GodModels.ViewModels
{
    [ProtoContract]
    public class ScheduleData
    {
        [ProtoMember(1)]
        public int TeamId { get; set; }
        [ProtoMember(2)]
        public int Week { get; set; }
        /// <summary>
        /// 1=周一，7=周日
        /// </summary>
        [ProtoMember(3)]
        public int Day { get; set; }
        [ProtoMember(4)]
        public ScheduleTypeEnum Type { get; set; }
        [ProtoMember(5)]
        public ScheduleStateEnum State { get; set; }
        /// <summary>
        /// 比赛或其他
        /// </summary>
        [ProtoMember(6)]
        public int TargetId { get; set; }
        [ProtoMember(7)]
        public int Grade { get; set; }
        /// <summary>
        /// 比赛胜利方
        /// </summary>
        [ProtoMember(8)]
        public int Result { get; set; }
        /// <summary>
        /// 增减粉丝
        /// </summary>
        [ProtoMember(9)]
        public int Fans { get; set; }
        [ProtoMember(10)]
        public int Key { get; set; }

    }
}
