using System;
using System.Net.WebSockets;
using CommonLibs;

namespace CommonNetwork
{
    public interface IUserManager<T>
    {
        int GetSocketUserCount();
        int GetTotalUserCount();
        T UpdateUser(UserConnTypeEnum connType, string channel, int userId, UserTypeEnum userType, int roleId, string token, double expiresIn);
        int RemoveUser(string channel);
        bool RemoveUserById(int id);
        bool ValidChannel(string channel);
        T GetUserData(string channel);
        T GetUserDataById(int id);
    }
}
