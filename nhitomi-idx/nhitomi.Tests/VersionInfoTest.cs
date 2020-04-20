using System;
using NUnit.Framework;

namespace nhitomi.Tests
{
    public class VersionInfoTest
    {
        [Test]
        public void Valid()
        {
            var commit = VersionInfo.Commit;

            Assert.That(commit, Is.Not.Null);
            Assert.That(commit.Hash, Is.Not.Null.Or.EqualTo("<unknown>"));
            Assert.That(commit.ShortHash, Is.EqualTo(commit.Hash.Substring(0, 7)));
            Assert.That(commit.Author, Is.Not.Null);
            Assert.That(commit.Time, Is.Not.EqualTo(default(DateTime)));
            Assert.That(commit.Message, Is.Not.Null);
        }
    }
}