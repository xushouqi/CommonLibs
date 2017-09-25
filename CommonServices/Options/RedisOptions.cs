using System;
using System.Collections.Generic;
using System.Text;

namespace CommonServices
{
    public class RedisServer
    {
        public string Connection { get; set; }
        public int DefaultDatabase { get; set; } = 0;
    }

    public class RedisOptions
    {        
        public Dictionary<string, RedisServer> RedisServers { get; set; }
    }
}
