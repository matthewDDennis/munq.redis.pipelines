using System.Threading.Tasks;

namespace Munq.Redis.Client
{
    public static class RedisResponseReader
    {
        public static Task<object> ReadRedisResponseAsync(this RedisConnection connection)
        {
            return Task.FromResult("OK" as object);
        }
    }
}
