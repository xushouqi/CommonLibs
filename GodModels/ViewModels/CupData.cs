using ProtoBuf;

namespace GodModels.ViewModels
{
    [ProtoContract]
    public class CupData
    {
        [ProtoMember(1)]
        public int Key { get; set; }
        [ProtoMember(2)]
        public string Name { get; set; }
        /// <summary>
        /// 杯赛级别
        /// </summary>
        [ProtoMember(3)]
        public int Grade { get; set; }
        /// <summary>
        /// 第几届
        /// </summary>
        [ProtoMember(4)]
        public int Season { get; set; }
        /// <summary>
        /// 当前轮次
        /// </summary>
        [ProtoMember(5)]
        public CupStageEnum Stage { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        [ProtoMember(6)]
        public CupStateEnum State { get; set; }
        /// <summary>
        /// 下次更新状态的时间
        /// </summary>
        [ProtoMember(7)]
        public System.DateTime NextUpdateTime { get; set; }
        /// <summary>
        /// 报名费
        /// </summary>
        [ProtoMember(8)]
        public int SignUpFee { get; set; }

    }
}
