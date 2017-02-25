# Creating a Redis Client using the .NET CorefxLabs System.IO.Pipelines Library

This article is the first in a series about creating an Asynchronous client for the Redis server that is low allocation, and hence GC pressure, with minimal data copying.  This is done using the techniques used to make Kestrel one of the top ten web servers in terms of raw requests per second as recorded in [Round 13 of the TechEmpower Plain Text](https://www.techempower.com/benchmarks/#section=data-r13&hw=ph&test=plaintext) performance tests.

The code, and this article, are being maintained on GitHub at https://github.com/matthewDDennis/munq.redis.pipelines. For reference, the old, non-Pipelines project can be found at https://github.com/matthewDDennis/Munq.Redis.

In a previous article, [Running Redis as a Windows Service](https://www.codeproject.com/Articles/715967/Running-Redis-as-a-Windows-Service), I showed how to, well as the title says.

## Background

Sometime ago, I started writing an Async, .NET Core Redis Client.  At the time, none of the Redis Clients supported .NET Core, and I wanted to write an article on how to implement a client for a simple protocol.  

Unfortunately, the changes from VS2015 RC1 and RC2 showed that the platform was going to be unstable for sometime, and while I had a fairly complete implementation,  I put it on the shelf until thing in the .NET and Visual Studio world became more stable.

With the upcoming release of VS2017 and the stablization of the CLI, NetStandard, and tooling I think it is time to revisit this project.  One thing that has peaked my interest in the .NET Core has been how much the performance has improved, particularily around the Kestrel web server performance.

The .NET Core team, and in particular David Fowler, have taken what they learned improving Kestrel, and created a set of libraries that allow for the processing streams of data in a manner that has little or no memory allocations, and minimal data copying.  This is done by reversing the existing Stream paradigm so that instead of pushing and pulling data buffers into and out of streams, the data buffers are managed by the low level APIs and pushed up to the application.  These use highly efficeint memory buffer pools and structures to achieve performance that has made Kestrel one of the fastest web servers availble.  You can see the code for these libraries at http://www.github.com/dotnet/corefxlab.

It should be noted that the code in the corefxlabs is where the .NET team experiment with new ideas, and as such any of the libraries are not guaranteed to be officially released, and if they are, their APIs will probably change.  Aslo, there is little or no documentation other than the code and some samples.

That being said, it appears that [Kestrel](https://github.com/aspnet/KestrelHttpServer) is being modified to use the System.IO.Pipelines package, and it also being used in next version of [SignalR](http://www.github.com/aspnet/signalr).

## Introduction
Several years ago, here at CodeProject, we took a look at the performance of our web page response time, and found it severly lacking. On each request, we were doing database requests for commonly requested data, and performing complex and expensive sanitization and formatting of content.

We embarked on a project to use Caching of various kinds of information and view models to improve the performance of the site.  This caching needed to be distributed so that all the servers in our web farm would stay consitent and current with the latest data.  After evaluating serveral options, we decided on [Redis](http://redis.io) due to its speed, cost, wide adoption, great reviews, and the power of its data structures and API.

The resulting preformance improvement exceeded our expections, and pages that were taking seconds, and even tens of seconds, were being returned in less a second, usually less than 500 mS, and greatly reduces the CPU load on our SQL Server.  Further performance improvements have been acheived by adding background event processing and the optimization of some expensive and heavily use algorithms, but I doubt that anything we can do will generate the improvements we obtained by using Redis.

Our current implementation use the ServiceStack Redis Client V3.  I have had to look into its code to resolve a number of issues, and as any programmer would, decided I can do it better, or at least differntly.  This is mainly due to improvements in the C# language, such as Extension Methods.  This allow me to create a small client that just sends and receives stuff to and from the Redis Server. The actual commands are implemented using Extension Methods.  This eliminates the hugh classes in the Service Stack implementation allowing for greater Single Resposibility of each class.

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

##Software Design
The Redis Client library will be implemented using Visual Studio 2017 RC4 (or RTM if this takes time) as a .NET Core class library in C#.

Class | Description
------|------------


