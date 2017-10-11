using ProtoBuf;

namespace GodModels.ViewModels
{
    [ProtoContract]
    public class TransferListingData
    {
        [ProtoMember(1)]
        public int ID { get; set; }
        [ProtoMember(2)]
        public TransferListingStateEnum State { get; set; }
        [ProtoMember(3)]
        public int TeamId { get; set; }
        [ProtoMember(4)]
        public int PlayerId { get; set; }
        /// <summary>
        /// 报价
        /// </summary>
        [ProtoMember(5)]
        public int Price { get; set; }
        /// <summary>
        /// 最终交易队伍
        /// </summary>
        [ProtoMember(6)]
        public int FinishTeamId { get; set; }
        [ProtoMember(7)]
        public int Key { get; set; }

    }
}
