using System.Net;
using System.Threading.Tasks;

namespace Munq.Redis.Client
{
    public interface IRedisConnectionfactory
    {
        Task<RedisConnection> CreateAsync(string host, int port, int database);
        Task<RedisConnection> CreateAsync(IPAddress ipAddress, int port, int database);
        Task<RedisConnection> CreateAsync(IPEndPoint endpoint, int database);
    }
}
