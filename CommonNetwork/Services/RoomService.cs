using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Threading;
using System.Net.WebSockets;
using CommonLibs;

namespace CommonNetwork
{
    [WebSocket]
    public class RoomService
    {
        private readonly ILogger m_logger;
        private readonly IRoomManager m_roomManager;
        private readonly IUserManager<UserData> m_userManager;

        public RoomService(ILoggerFactory logService,
            IUserManager<UserData> userManager,
            IRoomManager roomService)
        {
            m_logger = logService.CreateLogger("RoomService");
            m_roomManager = roomService;
            m_userManager = userManager;

            m_logger.LogInformation("RoomService Start, {0}", Thread.CurrentThread.ManagedThreadId);
        }

        [Api(ActionId = 101, AuthPolicy = UserTypeEnum.Member, Tips = "匹配游戏")]
        public async Task<ReturnData<bool>> SearchRoom(int accountId)
        {
            var retData = new ReturnData<bool>(true);
            var userData = m_userManager.GetUserDataById(accountId);
            if (userData != null)
            {
                retData.Data = m_roomManager.AddToWaitingList(userData);
            }
            return await Task.FromResult(retData);
        }

        [Api(ActionId = 102, Tips = "进入房间（服务端推送）")]
        public async Task<ReturnData<RoomData>> OnEnterRoom(int accountId)
        {
            var retData = new ReturnData<RoomData>();
            return await Task.FromResult(retData);
        }

    }
}
