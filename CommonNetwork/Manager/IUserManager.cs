using System;
using System.Net.WebSockets;
using CommonLibs;

namespace CommonNetwork
{
    public interface IUserManager<T>
    {
        void AddOnUpdateUser(Action<UserData> callback);
        void RemoveOnUpdateUser(Action<UserData> callback);
        void AddOnRemoveUser(Action<int> callback);
        void RemoveOnRemoveUser(Action<int> callback);

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
