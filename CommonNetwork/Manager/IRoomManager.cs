using System;
using CommonLibs;

namespace CommonNetwork
{
    public interface IRoomManager
    {
        RoomBase CreateRoom(UserData userData);
        RoomBase GetRoom(int id);
        bool RemoveRoom(int id);
        bool AddToWaitingList(UserData userData);
    }
}
