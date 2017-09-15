
namespace CommonServices.Caching
{
    public class HybridCacheClientOptions : CacheClientOptionsBase
    {
        public bool EnableDistributeCache { get; set; } = true;
    }
}