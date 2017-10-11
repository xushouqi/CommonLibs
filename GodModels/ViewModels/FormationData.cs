using ProtoBuf;

namespace GodModels.ViewModels
{
    [ProtoContract]
    public class FormationData
    {
        /// <summary>
        /// 队伍ID
        /// </summary>
        [ProtoMember(1)]
        public int TeamId { get; set; }
        /// <summary>
        /// 阵容编号
        /// </summary>
        [ProtoMember(2)]
        public int Sequence { get; set; }
        /// <summary>
        /// 队员ID
        /// </summary>
        [ProtoMember(3)]
        public int PlayerId { get; set; }
        /// <summary>
        /// 阵容位置
        /// </summary>
        [ProtoMember(4)]
        public int Position { get; set; }
        [ProtoMember(5)]
        public int Key { get; set; }

    }
}
