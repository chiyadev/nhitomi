using System.Threading.Tasks;
using nhitomi.Core.Clients;
using NUnit.Framework;

namespace nhitomi.Core.UnitTests.Clients
{
    public class ClientTests
    {
        [Test]
        public async Task nhentaiClient()
        {
            var tester = new ClientTester();

            await tester.TestAsync()
        }
    }
}
