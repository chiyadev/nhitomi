using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Database;
using NUnit.Framework;

namespace nhitomi.Tests
{
    [TestFixture(typeof(RedisCacheManager))]
    public class CacheStoreTest<T> : TestBaseServices where T : ICacheManager
    {
        [Test]
        public async Task Get()
        {
            var manager = ActivatorUtilities.CreateInstance<T>(Services);

            var store = manager.CreateStore<string>();

            var value = await store.GetAsync("key", () => Task.FromResult("value"));

            Assert.That(value, Is.EqualTo("value"));

            value = await store.GetAsync("key", () => Task.FromResult("value"));

            Assert.That(value, Is.EqualTo("value"));
        }

        [Test]
        public async Task GetCached()
        {
            var manager = ActivatorUtilities.CreateInstance<T>(Services);

            var store = manager.CreateStore<string>();

            await store.GetAsync("key", () => Task.FromResult("value"));

            var value = await store.GetAsync("key", () => Task.FromResult("second value"));

            Assert.That(value, Is.EqualTo("value"));
        }

        [Test]
        public async Task Penetration()
        {
            var manager = ActivatorUtilities.CreateInstance<T>(Services);

            var store = manager.CreateStore<string>();

            await store.GetAsync("key", () => Task.FromResult(null as string));

            var value = await store.GetAsync("key", () => Task.FromResult("value"));

            Assert.That(value, Is.Null);
        }

        [Test]
        public async Task Expiry()
        {
            var manager = ActivatorUtilities.CreateInstance<T>(Services);

            var expiry = TimeSpan.FromMilliseconds(1);
            var store  = manager.CreateStore<string>(default, expiry);

            await store.GetAsync("key", () => Task.FromResult("value"));

            await Task.Delay(expiry);

            var value = await store.GetAsync("key", () => Task.FromResult("second value"));

            Assert.That(value, Is.EqualTo("second value"));
        }

        [Test]
        public async Task Delete()
        {
            var manager = ActivatorUtilities.CreateInstance<T>(Services);

            var store = manager.CreateStore<string>();

            var value = await store.SetAsync("key", "value");

            Assert.That(value, Is.EqualTo("value"));

            value = await store.DeleteAsync("key");

            Assert.That(value, Is.EqualTo("value"));

            value = await store.GetAsync("key", () => Task.FromResult("value"));

            Assert.That(value, Is.Null);
        }
    }
}