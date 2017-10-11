using ProtoBuf;

namespace GodModels.ViewModels
{
    [ProtoContract]
    public class MatchEventData
    {
        [ProtoMember(1)]
        public MatchEventTypeEnum Type { get; set; }
        [ProtoMember(2)]
        public int EventId { get; set; }
        [ProtoMember(3)]
        public int Key { get; set; }

    }
}
