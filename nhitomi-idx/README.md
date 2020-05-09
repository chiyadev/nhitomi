# nhitomi-idx

This is nhitomi backend server written in C#.

### Prerequisites

- [.NET Core SDK 3.1+](https://dotnet.microsoft.com/download)
- [Elasticsearch 7+](https://www.elastic.co/downloads/elasticsearch)
- [Redis 4+](https://redis.io/download)

### Testing

[nhitomi.Tests](nhitomi.Tests) contains tests written using NUnit. This includes many integration tests that depend on Elasticsearch and Redis.

It is strongly recommend to have a locally running Elasticsearch instance to speed up the testing time. Each test initializes a new set of indexes with a random prefix to avoid collision with other tests, and are automatically deleted when finished. Redis must also be running (easily installable on Linux; using [WSL](https://en.wikipedia.org/wiki/Windows_Subsystem_for_Linux) on Windows).
