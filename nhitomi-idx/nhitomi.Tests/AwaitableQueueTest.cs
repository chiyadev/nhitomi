using System.Threading;
using NUnit.Framework;

namespace nhitomi.Tests
{
    [Parallelizable(ParallelScope.All)]
    public class AwaitableQueueTest
    {
        [Test]
        public void DequeueWaiting()
        {
            var queue = new AwaitableQueue<string>();

            var task1 = queue.DequeueAsync();
            var task2 = queue.DequeueAsync();
            var task3 = queue.DequeueAsync();

            queue.Enqueue("1");
            queue.Enqueue("2");

            Assert.That(task1.IsCompletedSuccessfully, Is.True);
            Assert.That(task1.Result, Is.EqualTo("1"));

            Assert.That(task2.IsCompletedSuccessfully, Is.True);
            Assert.That(task2.Result, Is.EqualTo("2"));

            Assert.That(task3.IsCompleted, Is.False);

            queue.Enqueue("3");

            Assert.That(task3.IsCompletedSuccessfully, Is.True);
            Assert.That(task3.Result, Is.EqualTo("3"));
        }

        [Test]
        public void DequeueEnqueued()
        {
            var queue = new AwaitableQueue<string>();

            queue.Enqueue("1");
            queue.Enqueue("2");

            var task1 = queue.DequeueAsync();
            var task2 = queue.DequeueAsync();
            var task3 = queue.DequeueAsync();

            Assert.That(task1.IsCompletedSuccessfully, Is.True);
            Assert.That(task1.Result, Is.EqualTo("1"));

            Assert.That(task2.IsCompletedSuccessfully, Is.True);
            Assert.That(task2.Result, Is.EqualTo("2"));

            Assert.That(task3.IsCompleted, Is.False);

            queue.Enqueue("3");

            Assert.That(task3.IsCompletedSuccessfully, Is.True);
            Assert.That(task3.Result, Is.EqualTo("3"));
        }

        [Test]
        public void CancellationWaiting()
        {
            var queue = new AwaitableQueue<string>();

            using var cts = new CancellationTokenSource();

            var task = queue.DequeueAsync(cts.Token);

            Assert.That(task.IsCompleted, Is.False);

            cts.Cancel();

            Assert.That(task.IsCanceled, Is.True);
        }

        [Test]
        public void CancellationEnqueued()
        {
            var queue = new AwaitableQueue<string>();

            queue.Enqueue("1");

            using var cts = new CancellationTokenSource();

            var task = queue.DequeueAsync(cts.Token);

            Assert.That(task.IsCompleted, Is.True);
            Assert.That(task.Result, Is.EqualTo("1"));
        }

        [Test]
        public void CancellationAlready()
        {
            var queue = new AwaitableQueue<string>();

            using var cts = new CancellationTokenSource();

            cts.Cancel();

            var task = queue.DequeueAsync(cts.Token);

            Assert.That(task.IsCanceled, Is.True);
        }

        [Test]
        public void CancellationMix()
        {
            var queue = new AwaitableQueue<string>();

            using var cts = new CancellationTokenSource();

            var task1 = queue.DequeueAsync(CancellationToken.None);
            var task2 = queue.DequeueAsync(cts.Token);
            var task3 = queue.DequeueAsync(cts.Token);
            var task4 = queue.DequeueAsync(CancellationToken.None);
            var task5 = queue.DequeueAsync(cts.Token);

            cts.Cancel();

            queue.Enqueue("1");
            queue.Enqueue("2");
            queue.Enqueue("3");
            queue.Enqueue("4");
            queue.Enqueue("5");

            Assert.That(task1.IsCompleted, Is.True);
            Assert.That(task1.Result, Is.EqualTo("1"));

            Assert.That(task2.IsCanceled, Is.True);

            Assert.That(task3.IsCanceled, Is.True);

            Assert.That(task4.IsCompleted, Is.True);
            Assert.That(task4.Result, Is.EqualTo("2"));

            Assert.That(task5.IsCanceled, Is.True);

            Assert.That(queue.Count, Is.EqualTo(3));
        }
    }
}