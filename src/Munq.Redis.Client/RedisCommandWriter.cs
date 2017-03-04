using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Text.Formatting;
using System.Text.Utf8;
using System.Threading.Tasks;

namespace Munq.Redis.Client
{
    public static class RedisCommandWriter
    {
        /// <summary>
        /// Sends a command and it's parameters to the Stream.
        /// </summary>
        /// <param name="connection">The connection to the Redis Server.</param>
        /// <param name="command">The command.</param>
        /// <param name="parameters">The paramaters for the command.</param>
        public static Task WriteCommandAsync(this RedisConnection connection,
                                                  string command,
                                                  params object[] parameters)
        {
            return WriteCommandAsync(connection, command, parameters.AsEnumerable());
        }

        /// <summary>
        /// Sends a command and it's parameters to the Stream.
        /// </summary>
        /// <param name="connection">The connection to the Redis Server.</param>
        /// <param name="command">The command.</param>
        /// <param name="parameters">The paramaters for the command.</param>
        public static async Task WriteCommandAsync(this RedisConnection connection,
                                                  string command,
                                                  IEnumerable<object> parameters = null)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var sizeOfCommandArray = 1 + (parameters?.Count() ?? 0);
            WritableBuffer output  = connection.Output.Alloc();

            // output the command array start
            output.Write(RedisProtocol.Utf8ArrayStart);
            output.Append(sizeOfCommandArray, TextEncoder.Utf8);
            output.Write(RedisProtocol.Utf8CRLF);

            // output the command
            var commandData = (Utf8String)command;

            WriteRedisBulkString(output, commandData);

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
                output.Write(RedisProtocol.Utf8NullString);
            }

            var objType = value.GetType();

            if (objType == typeof(string))
                WriteRedisBulkString(output, (Utf8String) (value as string));
            else if (objType == typeof(byte[]))
                WriteRedisBulkString(output, new Utf8String(value as byte[]));
            else if (objType == typeof(bool))
                WriteRedisBulkString(output, (bool)value ? RedisProtocol.Utf8RedisTrue 
                                                         : RedisProtocol.Utf8RedisFalse);
            else
                WriteRedisBulkString(output, (Utf8String)(value.ToString()));
        }

        /// <summary>
        /// Writes a string as a RedisBulkString to the Stream.
        /// </summary>
        /// <param name="str">The string to write.</param>
        static void WriteRedisBulkString(WritableBuffer output, Utf8String str)
        {
            output.Write(RedisProtocol.Utf8BulkStringStart);
            output.Append(str.Length,      TextEncoder.Utf8);
            output.Write(str);
            output.Write(RedisProtocol.Utf8CRLF);
        }
    }
}
