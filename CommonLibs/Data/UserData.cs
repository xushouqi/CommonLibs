using System;

namespace CommonLibs
{
    public enum UserConnTypeEnum
    {
        WebApi = 0,
        WebSocket,
        Tcp,
    }

    public class UserData
    {
        public int ID { get; set; }
        public string Channel { get; set; }
        public string Jti { get; set; }
        public DateTime ExpireTime { get; set; }
        public UserTypeEnum Type { get; set; }
        public int RoleId { get; set; }
        public UserConnTypeEnum ConnType { get; set; } = UserConnTypeEnum.WebApi;
    }

}
