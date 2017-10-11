using ProtoBuf;

namespace GodModels.ViewModels
{
    [ProtoContract]
    public class CupApplyData
    {
        [ProtoMember(1)]
        public int Key { get; set; }

    }
}
