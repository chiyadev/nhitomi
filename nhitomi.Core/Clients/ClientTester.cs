using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
                    var x = await client.GetAsync(testCase.DoujinId, cancellationToken);
                    var y = testCase.KnownValue;

                    // compare the retrieved doujin with the known value
                    Compare(x.GalleryUrl, y.GalleryUrl, nameof(DoujinInfo.GalleryUrl));
                    Compare(x.PrettyName, y.PrettyName, nameof(DoujinInfo.PrettyName));
                    Compare(x.OriginalName, y.OriginalName, nameof(DoujinInfo.OriginalName));
                    Compare(x.UploadTime, y.UploadTime, nameof(DoujinInfo.UploadTime));
                    Compare(x.SourceId, y.SourceId, nameof(DoujinInfo.SourceId));
                    Compare(x.Artist, y.Artist, nameof(DoujinInfo.Artist));
                    Compare(x.Group, y.Group, nameof(DoujinInfo.Group));
                    Compare(x.Scanlator, y.Scanlator, nameof(DoujinInfo.Scanlator));
                    Compare(x.Language, y.Language, nameof(DoujinInfo.Language));
                    Compare(x.Characters, y.Characters, nameof(DoujinInfo.Characters));
                    Compare(x.Categories, y.Categories, nameof(DoujinInfo.Categories));
                    Compare(x.Tags, y.Tags, nameof(DoujinInfo.Tags));
                    Compare(x.PageCount, y.PageCount, nameof(DoujinInfo.PageCount));
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

        static void Compare<T>(T x, T y, string propertyName)
        {
            if (Equals(x, y))
                return;

            throw new ClientTesterException($"Property '{propertyName}' did not match: '{x}' != '{y}'");
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        static void Compare<T>(IEnumerable<T> x, IEnumerable<T> y, string propertyName) where T : IEquatable<T>
        {
            if (Equals(x, y))
                return;
            if (x != null && y != null && x.OrderlessEquals(y))
                return;

            throw new ClientTesterException($"Property '{propertyName}' did not match: " +
                                            $"'{(x == null ? "<null>" : string.Join("', '", x))}' != '{(y == null ? "<null>" : string.Join("', '", y))}'");
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
                    throw new ClientTesterException("Exception during client testing.", exceptions[0]);

                default:
                    throw new AggregateException(exceptions);
            }
        }
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