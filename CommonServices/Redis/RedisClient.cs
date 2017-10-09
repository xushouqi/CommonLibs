using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace CommonServices
{
    public class RedisClient : IDisposable
    {
        private RedisOptions _options;
        private ConcurrentDictionary<string, ConnectionMultiplexer> _connections;
        private RedisServer m_defaultServer = null;

        public RedisClient(IOptions<RedisOptions> config)
        {
            _options = config.Value;
            _connections = new ConcurrentDictionary<string, ConnectionMultiplexer>();
        }
        
        private RedisServer GetRedisServer(string instanceName)
        {
            RedisServer redisServer = null;
            if (string.IsNullOrEmpty(instanceName))
            {
                if (m_defaultServer == null && _options.RedisServers.Count > 0)
                {
                    RedisServer[] servers = new RedisServer[_options.RedisServers.Count];
                    _options.RedisServers.Values.CopyTo(servers, 0);
                    m_defaultServer = servers[0];
                }
                redisServer = m_defaultServer;
            }
            else if (!_options.RedisServers.TryGetValue(instanceName, out redisServer))
            {
                throw new ArgumentNullException($"Can't find redis server: {instanceName}!!!");
            }
            return redisServer;
        }
        private ConnectionMultiplexer GetConnect(string instanceName)
        {
            var redisServer = GetRedisServer(instanceName);
            return _connections.GetOrAdd(instanceName, p => ConnectionMultiplexer.Connect(redisServer.Connection));
        }
        
        public IDatabase GetDatabase(string instanceName = "", int? db = null)
        {
            if (!db.HasValue)
            {
                var redisServer = GetRedisServer(instanceName);
                if (redisServer != null)
                    db = redisServer.DefaultDatabase;
                else
                    db = 0;
            }
            return GetConnect(instanceName).GetDatabase(db.Value);
        }

        public IServer GetServer(string instanceName = "", int endPointsIndex = 0)
        {
            var redisServer = GetRedisServer(instanceName);
            var confOption = ConfigurationOptions.Parse((string)redisServer.Connection);
            return GetConnect(instanceName).GetServer(confOption.EndPoints[endPointsIndex]);
        }

        public ISubscriber GetSubscriber(string instanceName = "")
        {
            return GetConnect(instanceName).GetSubscriber();
        }

        public void Dispose()
        {
            if (_connections != null && _connections.Count > 0)
            {
                foreach (var item in _connections.Values)
                {
                    item.Close();
                }
            }
        }
    }
}