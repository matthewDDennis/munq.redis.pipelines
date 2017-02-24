using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO.Pipelines;
using System.IO.Pipelines.Text.Primitives;
using System.Text.Formatting;
using System.Text;
using System.Text.Utf8;

namespace Munq.Redis.Client
{
    public static class RedisCommandWriter
    {
        static readonly Utf8String CRLF       = (Utf8String)"\r\n";
        static readonly Utf8String NullString = (Utf8String)"$-1\r\n";
        static readonly Utf8String RedisTrue  = (Utf8String)"1";
        static readonly Utf8String RedisFalse = (Utf8String)"0";

        static readonly byte ArrayStart       = (byte)'*';
        static readonly byte BulkStringStart  = (byte)'$';

        /// <summary>
        /// Sends a command and it's parameters to the Stream.
        /// </summary>
        /// <param name="connection">The connection to the Redis Server.</param>
        /// <param name="command">The command.</param>
        /// <param name="parameters">The paramaters for the command.</param>
        public static Task WriteRedisCommandAsync(this RedisConnection connection,
                                                  string command,
                                                  params object[] parameters)
        {
            return WriteRedisCommandAsync(connection, command, parameters.AsEnumerable());
        }

        /// <summary>
        /// Sends a command and it's parameters to the Stream.
        /// </summary>
        /// <param name="connection">The connection to the Redis Server.</param>
        /// <param name="command">The command.</param>
        /// <param name="parameters">The paramaters for the command.</param>
        public static async Task WriteRedisCommandAsync(this RedisConnection connection,
                                                  string command,
                                                  IEnumerable<object> parameters = null)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var sizeOfCommandArray = 1 + (parameters?.Count() ?? 0);
            var output             = connection.Output.Alloc();

            output.Append(ArrayStart,         TextEncoder.Utf8);
            output.Append(sizeOfCommandArray, TextEncoder.Utf8);
            output.Append(CRLF,               TextEncoder.Utf8);

            if (sizeOfCommandArray > 1)
            {
                foreach (object obj in parameters)
                    WriteObject(output, obj);
            }
            await output.FlushAsync();
            // TODO: should I call this?
            // connection.Output.Complete();
        }

        /// <summary>
        /// Writes an object to the Stream.
        /// </summary>
        /// <param name="value">The object to add.</param>
        static void WriteObject(WritableBuffer output, object value)
        {
            if (value == null)
            {
                output.Append(CRLF, TextEncoder.Utf8);
            }

            var objType = value.GetType();

            if (objType == typeof(string))
                WriteRedisBulkString(output, (Utf8String) (value as string));
            else if (objType == typeof(byte[]))
                WriteRedisBulkString(output, new Utf8String(value as byte[]));
            else if (objType == typeof(bool))
                WriteRedisBulkString(output, (bool)value ? RedisTrue : RedisFalse);
            else
                WriteRedisBulkString(output, (Utf8String)(value.ToString()));
        }

        /// <summary>
        /// Writes a string as a RedisBulkString to the Stream.
        /// </summary>
        /// <param name="str">The string to write.</param>
        static void WriteRedisBulkString(WritableBuffer output, Utf8String str)
        {
            if (str != null)
            {
                output.Append(BulkStringStart, TextEncoder.Utf8);
                output.Append(str.Length,      TextEncoder.Utf8);
                output.Append(str,             TextEncoder.Utf8);
                output.Append(CRLF,            TextEncoder.Utf8);

            }
            else
            {
                output.Append(NullString,      TextEncoder.Utf8);
            }
        }
    }
}
