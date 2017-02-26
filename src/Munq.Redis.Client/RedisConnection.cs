using System.IO.Pipelines;

namespace Munq.Redis.Client
{
    /// <summary>
    /// This class implements the connection to the Redis Server by wrapping an
    /// IPipeConnection, usually a SocketConnection.  The wrapped connection is used
    /// to stream data to and from the Redis server.
    /// </summary>
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

        /// <summary>
        /// Gets the Input PipeReader from the underlying IPipeConnection.
        /// </summary>
        public IPipeReader Input  => _connection.Input;

        /// <summary>
        /// Gets the Output PipeWriter from the underlying IPipeConnection.
        /// </summary>
        public IPipeWriter Output => _connection.Output;

        /// <summary>
        /// Disposes of the Connection.
        /// </summary>
        public void Dispose()     => _connection.Dispose();
    }
}