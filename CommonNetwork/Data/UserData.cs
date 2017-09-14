using System;
using CommonLibs;

namespace CommonNetwork
{

    public class UserData
    {
        public int ID { get; set; }
        public int SocketHandle { get; set; }
        public string Token { get; set; }
        public DateTime ExpireTime { get; set; }
        public UserTypeEnum Type { get; set; }
        public int RoleId { get; set; }
    }

}
