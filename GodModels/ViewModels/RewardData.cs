using ProtoBuf;

namespace GodModels.ViewModels
{
    [ProtoContract]
    public class RewardData
    {
        [ProtoMember(1)]
        public int Key { get; set; }

    }
}
