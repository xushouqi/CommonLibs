using ProtoBuf;

namespace GodModels.ViewModels
{
    [ProtoContract]
    public class BattleConfigData
    {
        [ProtoMember(1)]
        public int Key { get; set; }

    }
}
