using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.IO.Pipelines.Networking.Sockets;
using System.Linq;
using System.Threading.Tasks;

namespace Munq.Redis.Client
{
    public class RedisClient
    {
        private readonly RedisConnection _connection;

        public RedisClient(RedisConnection connection)
        {
            _connection = connection;
        }

        public async Task WriteCommandAsync(string command, IEnumerable<object> parameters = null)
        {
            await EnsureDatabaseSelected();
            await _connection.WriteRedisCommandAsync(command, parameters);
        }

        private async Task EnsureDatabaseSelected()
        {
            if (!_connection.IsDatabaseSelected)
            {
                if (_connection.Database != 0)
                {
                    await _connection.WriteRedisCommandAsync("SELECT", _connection.Database);
                }
                _connection.IsDatabaseSelected = true;
            }
        }

        //public async Task<object> ReadResponseAsync()
        //{
        //    return _connection.ReadRedisResponseAsync();
        //}
    }
}
