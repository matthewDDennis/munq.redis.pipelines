# Creating a Redis Client using the .NET CorefxLabs System.IO.Pipelines Library

This article is the first in a series about creating an Asynchronous client for the Redis server that is low allocation, and hence GC pressure, with minimal data copying.  This is done using the techniques used to make Kestrel one of the top ten web servers in terms of raw requests per second as recorded in [Round 13 of the TechEmpower Plain Text](https://www.techempower.com/benchmarks/#section=data-r13&hw=ph&test=plaintext) performance tests.

The code, and this article, are being maintained on GitHub at https://github.com/matthewDDennis/munq.redis.pipelines. For reference, the old, non-Pipelines project can be found at https://github.com/matthewDDennis/Munq.Redis.

In a previous article, [Running Redis as a Windows Service](https://www.codeproject.com/Articles/715967/Running-Redis-as-a-Windows-Service), I showed how to, well as the title says.

## Background

Sometime ago, I started writing an Async, .NET Core Redis Client.  At the time, none of the Redis Clients supported .NET Core, and I wanted to write an article on how to implement a client for a simple protocol.  

Unfortunately, the changes from VS2015 RC1 and RC2 showed that the platform was going to be unstable for sometime, and while I had a fairly complete implementation,  I put it on the shelf until things in the .NET and Visual Studio world became more stable.

With the upcoming release of VS2017 and the stabilization of the CLI, NetStandard, and tooling I think it is time to revisit this project.  One thing that has peaked my interest in the .NET Core has been how much the performance has improved, particularly around the Kestrel web server performance.

The .NET Core team, and in particular David Fowler, have taken what they learned improving Kestrel, and created a set of libraries that allow for the processing streams of data in a manner that has little or no memory allocations, and minimal data copying.  This is done by reversing the existing Stream paradigm so that instead of pushing and pulling data buffers into and out of streams, the data buffers are managed by the low level APIs and pushed up to the application.  These use highly efficient memory buffer pools and structures to achieve performance that has made Kestrel one of the fastest web servers available.  You can see the code for these libraries at http://www.github.com/dotnet/corefxlab.

It should be noted that the code in the corefxlabs is where the .NET team experiment with new ideas, and as such any of the libraries are not guaranteed to be officially released, and if they are, their APIs will probably change.  Also, there is little or no documentation other than the code and some samples.

That being said, it appears that [Kestrel](https://github.com/aspnet/KestrelHttpServer) is being modified to use the `System.IO.Pipelines` package, and it also being used in next version of [SignalR](http://www.github.com/aspnet/signalr).

## Introduction
Several years ago, here at Code Project, we took a look at the performance of our web page response time, and found it severely lacking. On each request, we were doing database requests for commonly requested data, and performing complex and expensive sanitization and formatting of content.

We embarked on a project to use Caching of various kinds of information and view models to improve the performance of the site.  This caching needed to be distributed so that all the servers in our web farm would stay consistent and current with the latest data.  After evaluating several options, we decided on [Redis](http://redis.io) due to its speed, cost, wide adoption, great reviews, and the power of its data structures and API.

The resulting performance improvement exceeded our expectations, and pages that were taking seconds, and even tens of seconds, were being returned in less a second, usually less than 500 mS, and greatly reduced the CPU load on our SQL Server.  Further performance improvements have been achieved by adding background event processing and the optimization of some expensive and heavily use algorithms, but I doubt that anything we can do will generate the improvements we obtained by using Redis.

Our current implementation use the ServiceStack Redis Client V3.  I have had to look into its code to resolve a number of issues, and as any programmer would, decided I can do it better, or at least differently.  This is mainly due to improvements in the C# language, such as Extension Methods.  This allow me to create a small client that just sends and receives stuff to and from the Redis Server. The actual commands are implemented using Extension Methods.  This eliminates the huge classes in the Service Stack implementation allowing for greater Single Responsibility of each class.

The goals of this implementation are

- Simplicity
- Performance
- Efficiency
- Robustness
- Complete Unit Testing

## Redis Protocol
Clients communicate with the Redis Server using the REdis Serialization Protocol(RESP) as detailed in [The Redis Protocol Specification](https://redis.io/topics/protocol). As the specification states:

>Redis clients communicate with the Redis server using a protocol called **RESP**. (REdis Serialization Protocol). While the protocol was designed specifically for Redis, it can be used for other client-server software projects.
> 
>RESP is a compromise between the following things:
> - Simple to implement.
> - Fast to parse.
> - Human readable.
>
>RESP can serialize different data types like integers, strings, arrays. There is also a specific type for errors. Requests are sent from the client to the Redis server as arrays of strings representing the arguments of the command to execute. Redis replies with a command specific data type.
>
>RESP is binary-safe and does not require processing of bulk data transferred from one process to another, because it uses prefixed-length to transfer bulk data.

Rather than go into detail about the protocol, I'll leave it to the reader to reference the specification if you need to clarify anything about what I am doing.  It's small, simple, and easy to understand.  I'll explain the specific protocol details when I explain the code that uses them.

## Software Design
The Redis Client library will be implemented using Visual Studio 2017 RC4 (or RTM if this article takes time) as a .NET Core class library in C#.

Class | Description
------|------------
RedisClient | This class is the client that communicate with the Redis server.  It does this through two methods `WriteCommandAsync` and `ReadResponseAsync`.  The constructor for the class will take a `RedisConnection`. Alternately, the constructor will take an instance of an `IRedisConnectionFactory` and a Database number. The various [Redis Commands](https://redis.io/commands) will be implemented as extension methods to this class or an interface this class implements.
RedisConnection | A RedisConnection provides the communication channel to Redis Server and a specific Database in the server.  It wraps an IPipeConnection, from the `System.IO.Pipelines` library and a database number, both of which are passed in the constructor.  In normal operation, the `IPipeConnection` would be an instance of the `SocketConnection` class from the `System.IO.Pipelines.Networking.Sockets` library.  However, for testing other implementations, such as the `TestConnection` class, can be used.
IRedisConnectionFactory | This interface defines the API for creating a `RedisConnection`. The initial implementation will be a `SimpleRedisConnectionFactory` that creates a new connection for each call to Create.  Later we will implement a `PooledRedisConnectionFactory` that maintains a pool of live `RedisConnections` for each database. This is to improve performance by eliminating the cost of establishing the Socket connection for each `RedisClient` creation.
RedisCommandWriter | This class implements the basic functionality to format and send Redis commands to the Redis server. It is implemented as extension methods to the `RedisConnection`.
RedisResponseReader |This class implements the basic functionality to receive and parse Redis responses from the Redis server. It is implemented as extension methods to the `RedisConnection`.
SimpleRedisConnectionFactory | This is a simplistic implementation of an `IRedisConnectionFactory` that creates a new connection each time it 'creates' a new connection.

### Show me some code!
**RedisClient** is a simple class that allows the writing of commands to the Redis Server and reading responses from the server.  This accomplished by calling methods on the `RedisConnection`.

~~~ c#
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
~~~

**RedisConnection** wraps an `IPipeConnection` and adds properties to aid in ensuring that the correct Redis database is selected for the connection. It also implements the `IPipeConnection` interface by delegating to the wrapped `IPipeConnection`.  This allows the code for formatting and sending commands, and the code for recieving and parsing responses, to be written for a general `IPipeConnection` without knowing anything special about the `RedisConnection`.

Typically, the wrapped `IPipeConnection` is implemented by the `SocketConnection` class, but using the interface allows the classes to be tested without requiring a Socket connection.
~~~ c#
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
~~~





