using ProtoBuf;

namespace GodModels.ViewModels
{
    [ProtoContract]
    public class TransferBidData
    {
        [ProtoMember(1)]
        public int Key { get; set; }
        [ProtoMember(2)]
        public int ID { get; set; }
        [ProtoMember(3)]
        public TransferBidStateEnum State { get; set; }
        [ProtoMember(4)]
        public int TeamId { get; set; }
        [ProtoMember(5)]
        public int PlayerId { get; set; }
        /// <summary>
        /// 报价
        /// </summary>
        [ProtoMember(6)]
        public int Price { get; set; }
        /// <summary>
        /// 卖方
        /// </summary>
        [ProtoMember(7)]
        public int FromTeamId { get; set; }

    }
}
