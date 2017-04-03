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
            object result;

            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            ReadResult input = await connection.Input.ReadAsync();
            ReadableBuffer buffer = input.Buffer;


            var leadByte = buffer.First.Span[0];
            buffer = buffer.Slice(1);
            switch (leadByte)
            {
                case RedisProtocol.SimpleStringStart:
                    result = buffer.ReadSimpleStringAsync();
                    break;

                case RedisProtocol.ErrorStart:
                    result = buffer.ReadErrorStringAsync();
                    break;

                case RedisProtocol.NumberStart:
                    result = buffer.ReadLongAsync();
                    break;

                case RedisProtocol.BulkStringStart:
                    result = buffer.ReadBulkStringAsync();
                    break;

                case RedisProtocol.ArrayStart:
                    result = buffer.ReadArrayAsync();
                    break;

                default:
                    result = new RedisErrorString("Invalid response initial character " + (char)leadByte);
                    break;
            }

            return result;
        }

        private static object ReadSimpleStringAsync(this ReadableBuffer buffer)
        {
            // Find \n
            ReadCursor delim;
            ReadableBuffer line;

            if (!buffer.TrySliceTo((byte)'\r', (byte)'\n', out line, out delim))
            {
                return new RedisErrorString("Unable to read line");
            }
            PreservedBuffer preservedBuffer = line.Preserve();

            // Move the buffer to the rest
            buffer = buffer.Slice(delim).Slice(2);

            return preservedBuffer;
        }

        private static object ReadErrorStringAsync(this ReadableBuffer buffer)
        {
            throw new NotImplementedException();
        }

        private static object ReadLongAsync(this ReadableBuffer buffer)
        {
            throw new NotImplementedException();
        }

        private static object ReadBulkStringAsync(this ReadableBuffer buffer)
        {
            throw new NotImplementedException();
        }

        private static object ReadArrayAsync(this ReadableBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
