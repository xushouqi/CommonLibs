using System;

namespace CommonNetwork
{
    public class NettyOptions
    {
        public bool IsSsl { get; set; } = false;
        public int Port { get; set; } = 8099;
        public string Host { get; set; } = "127.0.0.1";
        public int BufferSize { get; set; } = 1024;
        public int HeartbeatInterval { get; set; } = 1000;
    }
}
