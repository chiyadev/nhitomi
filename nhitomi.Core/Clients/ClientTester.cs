using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
                    if (!doujin.Equals(testCase.KnownValue))
                        return false;
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
    }
}