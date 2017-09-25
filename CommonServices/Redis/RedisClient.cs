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

        public RedisClient(IOptions<RedisOptions> config)
        {
            _options = config.Value;
            _connections = new ConcurrentDictionary<string, ConnectionMultiplexer>();
        }
        
        private ConnectionMultiplexer GetConnect(string instanceName)
        {
            if (string.IsNullOrEmpty(instanceName) && _options.RedisServers.Count > 0)
            {
                instanceName = _options.RedisServers.Keys.GetEnumerator().Current;
            }
            if (!_options.RedisServers.TryGetValue(instanceName, out var redisServer))
            {
                throw new ArgumentNullException($"Can't find redis server: {instanceName}!!!");
            }
            return _connections.GetOrAdd(instanceName, p => ConnectionMultiplexer.Connect(redisServer.Connection));
        }
        
        public IDatabase GetDatabase(string instanceName = "", int? db = null)
        {
            if (!db.HasValue)
            {
                if (string.IsNullOrEmpty(instanceName) && _options.RedisServers.Count > 0)
                {
                    instanceName = _options.RedisServers.Keys.GetEnumerator().Current;
                }
                if (!_options.RedisServers.TryGetValue(instanceName, out var redisServer))
                {
                    throw new ArgumentNullException($"Can't find redis server: {instanceName}!!!");
                }
                if (redisServer != null)
                    db = redisServer.DefaultDatabase;
                else
                    db = 0;
            }
            return GetConnect(instanceName).GetDatabase(db.Value);
        }

        public IServer GetServer(string instanceName = "", int endPointsIndex = 0)
        {
            if (string.IsNullOrEmpty(instanceName) && _options.RedisServers.Count > 0)
            {
                instanceName = _options.RedisServers.Keys.GetEnumerator().Current;
            }
            if (!_options.RedisServers.TryGetValue(instanceName, out var redisServer))
            {
                throw new ArgumentNullException($"Can't find redis server: {instanceName}!!!");
            }
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