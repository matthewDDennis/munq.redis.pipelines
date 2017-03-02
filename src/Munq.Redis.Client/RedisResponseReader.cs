using System.Threading.Tasks;

namespace Munq.Redis.Client
{
    public static class RedisResponseReader
    {
        public static Task<object> ReadResponseAsync(this RedisConnection connection)
        {
            return Task.FromResult("OK" as object);
        }
    }
}
