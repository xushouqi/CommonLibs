using System;

namespace CommonLibs
{

    public class UserData
    {
        public int ID { get; set; }
        public int SocketHandle { get; set; }
        public string Jti { get; set; }
        public DateTime ExpireTime { get; set; }
        public UserTypeEnum Type { get; set; }
        public int RoleId { get; set; }
    }

}
