using Microsoft.Extensions.Logging;

namespace CommonServices.Caching
{
    public abstract class CacheClientOptionsBase
    {
        public ILoggerFactory LoggerFactory { get; set; }
    }
}