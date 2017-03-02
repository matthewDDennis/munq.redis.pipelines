using System;
using System.IO;
using System.IO.Pipelines;
using System.IO.Pipelines.Text.Primitives;
using System.Text.Utf8;
using System.Threading.Tasks;
using Xunit;

namespace Munq.Redis.Client.Tests
{
    public class ConnectionTests
    {
        [Fact]
        public async Task TestConnectionSendOutputToRemoteInput()
        {
            using (var connection = new TestConnection())
            {
                var expected = (Utf8String)"I'm watching the CBC National";
                var writeBuffer = connection.Output.Alloc();
                writeBuffer.Write(expected);
                await writeBuffer.FlushAsync();

                var readBuffer = await connection.RemoteInput.ReadAsync();
                var actual = readBuffer.Buffer.GetUtf8String();

                Assert.Equal(expected, (Utf8String)actual);
            }
        }

        [Fact]
        public async Task RedisConnectionHasDatabaseNotSelectedOnCreate()
        {
            var connectionFactory = new SimpleRedisConnectionFactory();
            using (var connection = await connectionFactory.CreateAsync("127.0.0.1", 135, 1))
            {
                Assert.Equal(1, connection.Database);
                Assert.False(connection.IsDatabaseSelected);
            }
        }

        [Fact]
        public async Task FirstWriteOnClientSendsSelectIfDatabaseNotZero()
        {
            using (var connection = new TestConnection())
            {
                string expected = "*1\r\n$4Ping\r\n";
                using (var redisConnection = new RedisConnection(connection, null, 1))
                {
                    await redisConnection.WriteCommandAsync("Ping");

                    var readBuffer = await connection.RemoteInput.ReadAsync();
                    var actual = readBuffer.Buffer.GetUtf8String();

                    Assert.Equal(expected, actual);
                }
            }
        }
    }
}
