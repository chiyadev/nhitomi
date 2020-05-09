using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace nhitomi
{
    /// <summary>
    /// <see cref="Extensions"/>
    /// </summary>
    [Parallelizable(ParallelScope.All)]
    public class ExtensionTest
    {
        [Flags]
        enum MyEnum
        {
            None = 0,
            One = 1,
            Two = 1 << 1,
            Three = 1 << 2,
            All = One | Two | Three,
            OneAndTwo = One | Two
        }

        [Test]
        public void ToFlags()
        {
            var flags = MyEnum.All.ToFlags();

            Assert.That(flags, Has.Exactly(3).Items);

            Assert.That(flags[0], Is.EqualTo(MyEnum.One));
            Assert.That(flags[1], Is.EqualTo(MyEnum.Two));
            Assert.That(flags[2], Is.EqualTo(MyEnum.Three));
        }

        [Test]
        public void ToBitwise()
        {
            var my = new[] { MyEnum.One, MyEnum.Two, MyEnum.None, MyEnum.OneAndTwo, MyEnum.None }.ToBitwise();

            Assert.That(my, Is.EqualTo(MyEnum.One | MyEnum.Two));
        }

        [Test]
        public void BufferEqualsTrue()
        {
            var a = new byte[] { 1, 2, 3, 4, 5 };
            var b = new byte[] { 1, 2, 3, 4, 5 };

            Assert.That(a.BufferEquals(b), Is.True);

            b[2] = 0;

            Assert.That(a.BufferEquals(b), Is.False);
        }

        [Test]
        public async Task SemaphoreDisposeInLock()
        {
            using var semaphore = new SemaphoreSlim(1);

            using (await semaphore.EnterAsync())
                semaphore.Dispose();
        }

        public enum MyNamedEnum
        {
            [EnumMember(Value = "named")] Named,
            [EnumMember] Invalid,
            Unnamed
        }

        [Test]
        public void GetEnumName()
        {
            Assert.That(MyNamedEnum.Named.GetEnumName(), Is.EqualTo("named"));
            Assert.That(MyNamedEnum.Invalid.GetEnumName(), Is.EqualTo(nameof(MyNamedEnum.Invalid)));
            Assert.That(MyNamedEnum.Unnamed.GetEnumName(), Is.EqualTo(nameof(MyNamedEnum.Unnamed)));
        }
    }
}