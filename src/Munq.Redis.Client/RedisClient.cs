using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Munq.Redis.Client
{
    /// <summary>
    /// This class provides the base functionality to send commands to a Redis Server and
    /// read the responses sent back.
    /// </summary>
    public class RedisClient
    {
        private readonly RedisConnection _connection;

        /// <summary>
        /// Initializes a new instance of the RedisConnection class.
        /// </summary>
        /// <param name="connection">
        /// The RedisConnection the client will use to communicate with the Redis Server.
        /// </param>
        public RedisClient(RedisConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Writes a command to the Redis Server.
        /// </summary>
        /// <param name="command">The Command to send.</param>
        /// <param name="parameters">The parameters to send with the command.</param>
        /// <returns>A task to await for the completion of the writing the command to the output buffer.</returns>
        public Task WriteCommandAsync(string command, IEnumerable<object> parameters = null)
        {
            // TODO: make sure the correct database is selected.  Should this happen here or in the
            //       RedisConnectionFactory or RedisConnection constructor?
            // await EnsureDatabaseSelected();
            return _connection.WriteRedisCommandAsync(command, parameters);
        }

        /// <summary>
        /// Writes a command to the Redis Server.
        /// </summary>
        /// <param name="command">The Command to send.</param>
        /// <param name="parameters">The parameters to send with the command.</param>
        /// <returns>A task to await for the completion of the writing the command to the output buffer.</returns>
        public Task WriteCommandAsync(string command, params object[] parameters)
        {
            // TODO: make sure the correct database is selected.  Should this happen here or in the
            //       RedisConnectionFactory or RedisConnection constructor?
            // await EnsureDatabaseSelected();
            return WriteCommandAsync(command, parameters.AsEnumerable());
        }

        /// <summary>
        /// Reads a response from the Redis Server and returns an object containing the response.
        /// </summary>
        /// <returns>A Task with a response object as its result.</returns>
        public Task<object> ReadResponseAsync()
        {
            return _connection.ReadRedisResponseAsync();
        }
    }
}
