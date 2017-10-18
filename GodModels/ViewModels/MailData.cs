using ProtoBuf;

namespace GodModels.ViewModels
{
    [ProtoContract]
    public class MailData
    {
        [ProtoMember(1)]
        public int Key { get; set; }
        /// <summary>
        ///  收件人TeamID
        /// </summary>
        [ProtoMember(2)]
        public int ToTeamId { get; set; }
        /// <summary>
        /// 发送者的ID
        /// </summary>
        [ProtoMember(3)]
        public int SenderId { get; set; }
        /// <summary>
        /// 发送者类型：玩家Role或选手Player
        /// </summary>
        [ProtoMember(4)]
        public int SenderType { get; set; }
        [ProtoMember(5)]
        public string Title { get; set; }
        [ProtoMember(6)]
        public string Content { get; set; }
        [ProtoMember(7)]
        public int Type { get; set; }
        [ProtoMember(8)]
        public int State { get; set; }
        /// <summary>
        /// 邮件待处理的目标ID，与邮件Type有关，如PlayerId, CupId
        /// </summary>
        [ProtoMember(9)]
        public int TargetId { get; set; }
        /// <summary>
        /// 附件奖励ID
        /// </summary>
        [ProtoMember(10)]
        public int RewardId { get; set; }

    }
}
