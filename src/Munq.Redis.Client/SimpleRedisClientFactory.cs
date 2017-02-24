using System;
using System.Collections.Generic;
using System.IO.Pipelines.Networking.Sockets;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Utf8;
using System.Text.Formatting;
using System.Threading.Tasks;
using System.IO.Pipelines;
using System.IO.Pipelines.Text.Primitives;

namespace Munq.Redis.Client
{
    public class SimpleRedisClientFactory : IRedisConnectionfactory
    {
        public async Task<RedisConnection> CreateAsync(string hostNameOrAddress, int port, int database)
        {
            var ipAddresses = await Dns.GetHostAddressesAsync(hostNameOrAddress);
            if (ipAddresses == null || ipAddresses.Length == 0)
                return null;

            return await CreateAsync(ipAddresses[0], port, database);
        }

        public Task<RedisConnection> CreateAsync(IPAddress ipAddress, int port, int database)
        {
            var endpoint = new IPEndPoint(ipAddress, port);
            return CreateAsync(endpoint, database);
        }

        public async Task<RedisConnection> CreateAsync(IPEndPoint endpoint, int database)
        {
            var socketConnection = await SocketConnection.ConnectAsync(endpoint);
            return new RedisConnection(socketConnection, database);
        }
    }
}
