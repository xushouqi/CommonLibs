//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using CommonNetwork.GodIdentity;
//using CommonNetwork.EClubServer;
//using CommonLibs;

//namespace CommonNetwork
//{
//    public enum NetworkStateEnum
//    {
//        None = 0,
//        ConnectingIDServer, //连接中
//        ConnectingGameServer,   //连接中
//        OnIDServer, //已连接ID服务器
//        LoginIDServer,  //已登录
//        OnGameServer,   //已连接游戏服务器
//        ValidGameServer,    //已验证身份
//        Reconnecting,   //重连中
//        OnError,    //网络错误
//    }

//    public class NetworkClinet
//    {
//        static object m_lock = new object();
//        private static NetworkClinet m_instance = null;
//        public static NetworkClinet Instance
//        {
//            get
//            {
//                if (m_instance == null)
//                {
//                    lock (m_lock)
//                    {
//                        if (m_instance == null)
//                            m_instance = new NetworkClinet();
//                    }
//                }
//                return m_instance;
//            }
//        }

//        /// <summary>
//        /// 连接成功
//        /// </summary>
//        public Action<bool, string, NetworkStateEnum> OnServerConnected;
//        /// <summary>
//        /// 断线
//        /// </summary>
//        public Action<string, NetworkStateEnum> OnServerDisconnect;
//        /// <summary>
//        /// 网络错误
//        /// </summary>
//        public Action<string, NetworkStateEnum> OnNetworkError;

//        /// <summary>
//        /// 是否在登录后自动连接游服
//        /// </summary>
//        public bool AutoConnGameServer = true;
//        /// <summary>
//        /// 是否断线后自动重连服务器
//        /// </summary>
//        public bool AutoReconnect = true;
//        /// <summary>
//        /// 自动重连服务器的次数
//        /// </summary>
//        public int MaxReconnectCount = 10;

//        public GameCache GameCache = null;
//        public ServerData MyGameServer = null;
//        public AccountData MyAccount = null;

//        public NetworkStateEnum MyState = NetworkStateEnum.None;

//        public UserClient UserClient = null;

//        private ClientSocket m_socket = null;

//        public NetworkClinet()
//        {
//            m_socket = new ClientSocket();
//            UserClient = new UserClient(m_socket);

//            m_socket.OnConnect = OnConnected;
//            m_socket.OnDisconnect = OnDisconnect;
//            m_socket.OnError = OnError;
//        }

//        private string m_id_addr = "127.0.0.1:20001";
//        private string m_game_addr = "127.0.0.1:20001";
//        private int m_reconn_count = 0;

//        /// <summary>
//        /// 连接登录服务器
//        /// </summary>
//        /// <param name="ipAddress"></param>
//        /// <param name="callback"></param>
//        public void ConnectLoginServer(string ipAddress, Action<bool, string> callback = null)
//        {
//            m_id_addr = ipAddress;
//            MyState = NetworkStateEnum.ConnectingIDServer;
//            m_socket.ConnectServer(m_id_addr, callback);
//        }

//        private async void OnConnected(bool success, string message)
//        {
//            if (success)
//            {
//                if (MyState == NetworkStateEnum.ConnectingIDServer)
//                {
//                    MyState = NetworkStateEnum.OnIDServer;
//                }
//                else if (MyState == NetworkStateEnum.ConnectingGameServer)
//                {
//                    MyState = NetworkStateEnum.OnGameServer;

//                    //验证token
//                    if (MyAccount != null && !string.IsNullOrEmpty(MyAccount.Token))
//                    {
//                        var ret = await EntryClient.SubmitValidAccountByTokenAsync(MyAccount.Token);
//                        OnValidToken(ret.ErrorCode, ret.Data);
//                    }
//                }
//            }
//            else
//            {
//                if (AutoReconnect && m_reconn_count > 0)
//                {
//                    m_reconn_count--;
//                    DoReconnect();
//                }
//                else
//                    MyState = NetworkStateEnum.None;
//            }
//            if (OnServerConnected != null)
//                OnServerConnected(success, message, MyState);
//        }
//        private void OnDisconnect(string message)
//        {
//            if (OnServerDisconnect != null)
//                OnServerDisconnect(message, MyState);
//        }
//        private void OnError(string message)
//        {
//            if (AutoReconnect)
//            {
//                m_reconn_count = MaxReconnectCount;
//                DoReconnect();
//            }
//            else
//                MyState = NetworkStateEnum.OnError;

//            if (OnNetworkError != null)
//                OnNetworkError(message, MyState);
//        }
//        private void DoReconnect()
//        {
//            if (MyState == NetworkStateEnum.OnIDServer || MyState == NetworkStateEnum.LoginIDServer || MyState == NetworkStateEnum.ConnectingIDServer)
//                ConnectLoginServer(m_id_addr);
//            else if (MyState == NetworkStateEnum.OnGameServer || MyState == NetworkStateEnum.ValidGameServer || MyState == NetworkStateEnum.ConnectingGameServer)
//                ConnectGameServer(m_game_addr);
//            else
//                MyState = NetworkStateEnum.OnError;
//        }

//        /// <summary>
//        /// 注册账号
//        /// </summary>
//        /// <param name="username"></param>
//        /// <param name="password"></param>
//        /// <param name="autoLogin">注册成功后是否自动登录</param>
//        /// <returns></returns>
//        public async Task<bool> RegisterAsync(string username, string password, bool autoLogin = false)
//        {
//            var ret = false;
//            if (MyState == NetworkStateEnum.OnIDServer)
//            {
//                var retData = await UserClient.SubmitRegisterAsync(username, password);
//                if (retData.ErrorCode == ErrorCodeEnum.Success)
//                {
//                    MyAccount = retData.Data;
//                    ret = true;

//                    if (autoLogin)
//                    {
//                        await LoginAsync(username, password, autoLogin);
//                    }
//                }
//            }
//            return ret;
//        }
//        /// <summary>
//        /// 登录
//        /// </summary>
//        /// <param name="username"></param>
//        /// <param name="password"></param>
//        /// <param name="autoConnGame">是否自动连接游服</param>
//        /// <returns></returns>
//        public async Task<bool> LoginAsync(string username, string password, bool autoConnGame = false)
//        {
//            var ret = false;
//            if (MyState == NetworkStateEnum.OnIDServer)
//            {
//                var retData = await UserClient.SubmitLoginAsync(username, password);
//                if (retData.ErrorCode == ErrorCodeEnum.Success)
//                {
//                    MyState = NetworkStateEnum.LoginIDServer;
//                    MyAccount = retData.Data;
//                    ret = true;

//                    AutoConnGameServer = autoConnGame;
//                    if (AutoConnGameServer)
//                    {
//                        //获取可用游戏服务器
//                        var retServer = await UserClient.SubmitGetCurrentServerAsync();
//                        if (retServer.ErrorCode == ErrorCodeEnum.Success)
//                        {
//                            MyGameServer = retServer.Data;
//                            if (!string.IsNullOrEmpty(MyGameServer.IPAddr))
//                            {
//                                await ConnectGameServer(MyGameServer.IPAddr);
//                            }
//                        }
//                    }
//                }
//            }
//            return ret;
//        }
//        public void CloseConnection()
//        {
//            MyState = NetworkStateEnum.None;
//            if (m_socket != null)
//                m_socket.CloseConnection();
//        }

//        public EntryClient EntryClient = null;
//        public RoleClient RoleClient = null;
//        public TeamClient TeamClient = null;
//        public MatchClient MatchClient = null;
//        public CareerClient CareerClient = null;
//        public PlayerClient PlayerClient = null;


//        /// <summary>
//        /// 连接游戏服务器
//        /// </summary>
//        /// <param name="address"></param>
//        public async Task ConnectGameServer(string address)
//        {
//            m_game_addr = address;
//            //todo: 断线
//            m_socket.CloseConnection();

//            //连接游戏服务
//            m_socket = new ClientSocket();

//            EntryClient = new EntryClient(m_socket);
//            RoleClient = new RoleClient(m_socket);
//            TeamClient = new TeamClient(m_socket);
//            MatchClient = new MatchClient(m_socket);
//            CareerClient = new CareerClient(m_socket);
//            PlayerClient = new PlayerClient(m_socket);


//            //游戏数据缓存
//            GameCache = new GameCache(this);

//            MyState = NetworkStateEnum.ConnectingGameServer;
//            var ret = await m_socket.ConnectServerAsync(address);
//            OnConnected(ret, "");
//        }
//        private void OnValidToken(ErrorCodeEnum error, AccountData account)
//        {
//            if (error == ErrorCodeEnum.Success)
//            {
//                MyState = NetworkStateEnum.ValidGameServer;

//                GameCache.MyRoleData = account.MyRole;
//                if (GameCache.MyRoleData != null)
//                    GameCache.MyTeamData = account.MyRole.MyTeam;
//            }
//        }

//        /// <summary>
//        /// 创建角色
//        /// </summary>
//        /// <param name="name"></param>
//        /// <returns></returns>
//        public async Task<bool> CreateRoleAsync(string name)
//        {
//            var ret = false;
//            if (MyState == NetworkStateEnum.ValidGameServer)
//            {
//                var retData = await RoleClient.SubmitCreateRoleAsync(name);
//                if (retData.ErrorCode == ErrorCodeEnum.Success)
//                {
//                    ret = true;
//                    GameCache.MyRoleData = retData.Data;
//                    GameCache.MyTeamData = retData.Data.MyTeam;
//                }
//            }
//            return ret;
//        }
//    }
//}
