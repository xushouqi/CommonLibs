using ProtoBuf;

namespace GodModels.ViewModels
{
    [ProtoContract]
    public class TeamUpgradeData
    {
        [ProtoMember(1)]
        public int Fans { get; set; }
        /// <summary>
        /// 生涯赛胜利获得粉丝数
        /// </summary>
        [ProtoMember(2)]
        public int WinFans { get; set; }
        [ProtoMember(3)]
        public int Key { get; set; }

    }
}
