using System.IO.Pipelines;
using System.Net;

namespace Munq.Redis.Client
{
    public interface IRedisConnection : IPipeConnection
    {
        IPEndPoint ServerEndPoint { get; }
        int Database { get; }
        bool IsDatabaseSelected { get; }
    }
}