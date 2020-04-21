using System;
using NUnit.Framework;

namespace nhitomi
{
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
    }
}