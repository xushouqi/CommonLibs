//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using CommonNetwork.GodIdentity;
//using CommonNetwork.EClubServer;
//using CommonLibs;

//namespace CommonNetwork
//{
//    public class GameCache
//    {
//        private int m_accountId = 0;
//        /// <summary>
//        /// 我的角色
//        /// </summary>
//        public RoleData MyRoleData = null;
//        /// <summary>
//        /// 我的队伍
//        /// </summary>
//        public TeamData MyTeamData = null;
//        /// <summary>
//        /// 我的选手
//        /// </summary>
//        public SortedDictionary<int, PlayerData> MyPlayers = new SortedDictionary<int, PlayerData>();
//        /// <summary>
//        /// 队伍缓存
//        /// </summary>
//        public AutoExpiryCache<int, TeamData> TeamCaches = new AutoExpiryCache<int, TeamData>("TeamData", 60);
//        /// <summary>
//        /// 选手缓存
//        /// </summary>
//        public AutoExpiryCache<int, PlayerData> PlayerCaches = new AutoExpiryCache<int, PlayerData>("PlayerData", 60);

//        private NetworkClinet m_networkClient;

//        public GameCache(NetworkClinet nclient)
//        {
//            m_networkClient = nclient;
//            m_accountId = m_networkClient.MyAccount.AccountId;

//            m_networkClient.RoleClient.AddToGetRole(OnGetRoleData);
//            m_networkClient.TeamClient.AddToGetTeam(OnGetTeamData);
//            m_networkClient.PlayerClient.AddToGetPlayer(OnGetPlayerData);
//        }

//        void OnGetRoleData(ErrorCodeEnum error, RoleData data)
//        {
//            if (data.AccountId == m_accountId)
//                MyRoleData = data;
//        }
//        void OnGetTeamData(ErrorCodeEnum error, TeamData data)
//        {
//            if (MyRoleData != null && data.RoleId == MyRoleData.ID)
//                MyTeamData = data;

//            TeamCaches.SetValue(data.ID, data);
//        }
//        void OnGetPlayerData(ErrorCodeEnum error, PlayerData data)
//        {
//            if (MyTeamData != null)
//            {
//                if (data.TeamId == MyTeamData.ID)
//                    MyPlayers[data.ID] = data;
//                else if (MyPlayers.ContainsKey(data.ID))
//                    MyPlayers.Remove(data.ID);
//            }

//            PlayerCaches.SetValue(data.ID, data);
//        }

//    }
//}
