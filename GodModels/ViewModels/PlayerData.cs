using ProtoBuf;

namespace GodModels.ViewModels
{
    [ProtoContract]
    public class PlayerData
    {
        [ProtoMember(1)]
        public int Key { get; set; }
        [ProtoMember(2)]
        public string Name { get; set; }
        [ProtoMember(3)]
        public int Sex { get; set; }
        [ProtoMember(4)]
        public PlayerPosEnum Position { get; set; }
        [ProtoMember(5)]
        public PlayerTypeEnum Type { get; set; }
        [ProtoMember(6)]
        public PlayerStateEnum State { get; set; }
        /// <summary>
        /// 合同剩余时间
        /// </summary>
        [ProtoMember(7)]
        public int ContractExpires { get; set; }
        /// <summary>
        /// 轮次薪水
        /// </summary>
        [ProtoMember(8)]
        public int Salary { get; set; }
        /// <summary>
        /// 签约金
        /// </summary>
        [ProtoMember(9)]
        public int SignFee { get; set; }
        /// <summary>
        /// 转会费
        /// </summary>
        [ProtoMember(10)]
        public int TransferFee { get; set; }
        [ProtoMember(11)]
        public int TeamId { get; set; }
        /// <summary>
        /// 名字前缀（队名）
        /// </summary>
        [ProtoMember(12)]
        public string NameExt { get; set; }
        /// <summary>
        /// 年龄（15~30），当前能力跟年龄和潜力有关
        /// </summary>
        [ProtoMember(13)]
        public int Age { get; set; }
        /// <summary>
        /// 潜力
        /// </summary>
        [ProtoMember(14)]
        public int Capacity { get; set; }
        /// <summary>
        /// 身价（综合计算值）
        /// </summary>
        [ProtoMember(15)]
        public int Value { get; set; }
        /// <summary>
        /// 星探评级（1-10分，对应1-5星）
        /// </summary>
        [ProtoMember(16)]
        public int Rate { get; set; }
        /// <summary>
        /// 技术
        /// </summary>
        [ProtoMember(17)]
        public int AttTechnique { get; set; }
        /// <summary>
        /// 意识
        /// </summary>
        [ProtoMember(18)]
        public int AttMentality { get; set; }
        /// <summary>
        /// 团队
        /// </summary>
        [ProtoMember(19)]
        public int AttTeamwork { get; set; }
        /// <summary>
        /// 心态
        /// </summary>
        [ProtoMember(20)]
        public int AttComposure { get; set; }

    }
}
