using System;
using System.IO.Pipelines;

namespace Munq.Redis.Client
{
    public class RedisConnection : IPipeConnection
    {
        private readonly IPipeConnection _connection;

        public RedisConnection(IPipeConnection connection, int database)
        {
            _connection        = connection;
            Database           = database;
            IsDatabaseSelected = false;
        }

        /// <summary>
        /// Gets the Database that the connection is talking to.
        /// </summary>
        /// <remarks>The Select command will change this.</remarks>
        public int Database { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the Database has been selected.
        /// </summary>
        public bool IsDatabaseSelected { get; internal set; }

        public IPipeReader Input  => _connection.Input;

        public IPipeWriter Output => _connection.Output;

        public void Dispose()     => _connection.Dispose();
    }
}