using ProtoBuf;

namespace GodModels.ViewModels
{
    [ProtoContract]
    public class TeamMoneyLogData
    {
        [ProtoMember(1)]
        public int Key { get; set; }

    }
}
