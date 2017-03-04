using System;
using System.Threading.Tasks;
using System.IO.Pipelines;
using System.IO.Pipelines.Text.Primitives;
using System.Text.Utf8;

namespace Munq.Redis.Client
{
    public static class RedisResponseReader
    {
        public static async Task<object> ReadResponseAsync(this RedisConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            ReadResult input = await connection.Input.ReadAsync();
            ReadableBuffer buffer = input.Buffer;


            var leadByte = buffer.Slice(0, 1).First.Span[0];
            switch (leadByte)
            {
                case RedisProtocol.SimpleStringStart:
                    return null;// return await ReadSimpleStringAsync().ConfigureAwait(false);

                case RedisProtocol.ErrorStart:
                    return null;// return await ReadErrorStringAsync().ConfigureAwait(false);

                case RedisProtocol.NumberStart:
                    return null;// return await ReadLongAsync().ConfigureAwait(false);

                case RedisProtocol.BulkStringStart:
                    return null;// return await ReadBulkStringAsync().ConfigureAwait(false);

                case RedisProtocol.ArrayStart:
                    return null;// return await ReadArrayAsync().ConfigureAwait(false);

                default:
                    return null;// return new RedisErrorString("Invalid response initial character " + c);
            }
        }
    }
}
