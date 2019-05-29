using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi.Core.Clients
{
    /// <summary>
    /// Used to test doujin clients.
    /// </summary>
    public class ClientTester
    {
        static readonly Dictionary<Type, ClientTestCase[]> _testCases = typeof(IDoujinClient).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass && t.IsSubclassOf(typeof(ClientTestCase)))
            .Select(t => (ClientTestCase) Activator.CreateInstance(t))
            .GroupBy(c => c.ClientType)
            .ToDictionary(g => g.Key, g => g.ToArray());

        public readonly ConcurrentQueue<Exception> Exceptions = new ConcurrentQueue<Exception>();

        public async Task<bool> TestAsync(IDoujinClient client, CancellationToken cancellationToken = default)
        {
            try
            {
                // no test cases found for this client
                if (!_testCases.TryGetValue(client.GetType(), out var testCases))
                    return true;

                foreach (var testCase in testCases)
                {
                    // retrieve doujin
                    var doujin = await client.GetAsync(testCase.DoujinId, cancellationToken);

                    // compare the retrieved doujin with the known value
                    if (!DoujinEquals(doujin, testCase.KnownValue))
                        throw new ClientTesterException(
                            $"Doujin '{testCase.DoujinId}' returned by {client.GetType().Name} did not pass test {testCase.GetType().Name}.");
                }

                return true;
            }
            catch (TaskCanceledException)
            {
                // don't catch cancellation exceptions
                throw;
            }
            catch (Exception e)
            {
                Exceptions.Enqueue(e);
                return false;
            }
        }

        public void ThrowExceptions()
        {
            var exceptions = new List<Exception>();

            while (Exceptions.TryDequeue(out var exception))
                exceptions.Add(exception);

            switch (exceptions.Count)
            {
                case 0:
                    return;

                case 1:
                    throw exceptions[0];

                default:
                    throw new AggregateException(exceptions);
            }
        }

        static bool DoujinEquals(DoujinInfo x, DoujinInfo y) =>
            x.GalleryUrl == y.GalleryUrl &&
            x.PrettyName == y.PrettyName &&
            x.OriginalName == y.OriginalName &&
            x.UploadTime == y.UploadTime &&
            x.SourceId == y.SourceId &&
            x.Artist == y.Artist &&
            x.Group == y.Group &&
            x.Scanlator == y.Scanlator &&
            x.Language == y.Language &&
            x.Parody == y.Parody &&
            x.Characters.OrderlessEquals(y.Characters) &&
            x.Categories.OrderlessEquals(y.Categories) &&
            x.Tags.OrderlessEquals(y.Tags) &&
            x.PageCount == y.PageCount;
    }

    [Serializable]
    public class ClientTesterException : Exception
    {
        public ClientTesterException()
        {
        }

        public ClientTesterException(string message) : base(message)
        {
        }

        public ClientTesterException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ClientTesterException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}