using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using nhitomi.Models;
using NUnit.Framework;

namespace nhitomi
{
    public class ModelSanitizerTest
    {
        [Test]
        public void EmptyString()
            => Assert.That(ModelSanitizer.Sanitize(""), Is.Null);

        [Test]
        public void TrimString()
            => Assert.That(ModelSanitizer.Sanitize("    f  "), Is.EqualTo("f"));

        [Test]
        public void WhiteSpaceString()
            => Assert.That(ModelSanitizer.Sanitize("     "), Is.Null);

        [Test]
        public void InvalidSpaces()
            => Assert.That(ModelSanitizer.Sanitize("  ​​​​​​​​​  ​​​​​​​​​​​​​​​​​​​​​​​​​​​​"), Is.Null); // there is zws in this string

        [Test]
        public void NullCharString()
            => Assert.That(ModelSanitizer.Sanitize($"test{(char) 0}"), Is.EqualTo("test"));

        struct TestStruct
        {
            public string String { get; set; }
        }

        [Test]
        public void IgnoreStruct()
        {
            var test = new TestStruct
            {
                String = " "
            };

            test = ModelSanitizer.Sanitize(test);

            Assert.That(test.String, Is.EqualTo(" "));
        }

        [Test]
        public void StringArray()
        {
            var list = new[]
            {
                "",
                "test",
                "   test 2",
                null
            };

            list = ModelSanitizer.Sanitize(list);

            Assert.That(list, Is.EqualTo(new[]
            {
                "test",
                "test 2"
            }));
        }

        [Test]
        public void StringCollection()
        {
            var col = new Collection<string>
            {
                "",
                "test",
                "   test 2",
                null
            };

            col = ModelSanitizer.Sanitize(col);

            Assert.That(col, Is.EqualTo(new Collection<string>
            {
                "test",
                "test 2"
            }));
        }

        [Test]
        public void StringDictionary()
        {
            var dict = new Dictionary<string, string>
            {
                [""]              = "gone",
                ["   "]           = "gone",
                ["valid"]         = "  cool  ",
                ["    valid    "] = "overridden",
                ["gone"]          = null
            };

            dict = ModelSanitizer.Sanitize(dict);

            Assert.That(dict, Is.EqualTo(new Dictionary<string, string>
            {
                ["valid"] = "cool"
            }));
        }

        [Test]
        public void NullableValue()
        {
            var value = 100 as int?;

            value = ModelSanitizer.Sanitize(value);

            Assert.That(value, Is.EqualTo(100));
        }

        [Test]
        public void NullableNull()
        {
            var value = ModelSanitizer.Sanitize(null as int?);

            Assert.That(value, Is.Null);
        }

        sealed class TestNullable
        {
            public int? Value { get; set; } = 100;

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public int? Null { get; set; }
        }

        [Test]
        public void Nullable()
        {
            var obj = ModelSanitizer.Sanitize(new TestNullable());

            Assert.That(obj.Value, Is.EqualTo(100));
            Assert.That(obj.Null, Is.Null);
        }

        enum TestEnum : short
        {
            Two = 2
        }

        [Test]
        public void EnumUndefined()
        {
            var value = (TestEnum) 999;

            value = ModelSanitizer.Sanitize(value);

            Assert.That(value, Is.EqualTo(default(TestEnum)));
        }

        [Test]
        public void EnumValid()
        {
            var value = TestEnum.Two;

            value = ModelSanitizer.Sanitize(value);

            Assert.That(value, Is.EqualTo(TestEnum.Two));
        }

        [Flags]
        enum TestEnumFlags : byte
        {
            None = 0,
            One = 1,
            Three = 1 << 3
        }

        [Test]
        public void EnumFlagsUndefined()
        {
            var value = TestEnumFlags.None | TestEnumFlags.One | TestEnumFlags.Three | (TestEnumFlags) (1 << 7);

            value = ModelSanitizer.Sanitize(value);

            Assert.That(value, Is.EqualTo(TestEnumFlags.One | TestEnumFlags.Three));
        }

        [Test]
        public void EnumFlagsNone()
        {
            var value = TestEnumFlags.None;

            value = ModelSanitizer.Sanitize(value);

            Assert.That(value, Is.EqualTo(TestEnumFlags.None));
        }

        class TestComplexType
        {
            public string String { get; set; } = "  sanitize me ";
            public object IgnoreMe { get; set; } = "   ignored  ";

            public SortedSet<string> StringSet { get; set; } = new SortedSet<string>
            {
                "",
                "hello"
            };

            public class NestedType
            {
                public string Name { get; set; }
                public Dictionary<string, NestedType> Dict { get; set; }
            }

            public NestedType[] Nested { get; set; } =
            {
                null,
                null,
                new NestedType
                {
                    Name = "one"
                },
                new NestedType
                {
                    Name = "two",
                    Dict = new Dictionary<string, NestedType>
                    {
                        ["three"] = new NestedType
                        {
                            Name = "three"
                        },
                        [""] = new NestedType
                        {
                            Name = "NO"
                        }
                    }
                }
            };
        }

        [Test]
        public void ComplexType()
        {
            var obj = new TestComplexType();

            ModelSanitizer.Sanitize(obj);

            Assert.That(obj, Is.Not.Null);
            Assert.That(obj.String, Is.EqualTo("sanitize me"));
            Assert.That(obj.IgnoreMe, Is.EqualTo("   ignored  "));
            Assert.That(obj.StringSet, Is.EqualTo(new SortedSet<string> { "hello" }));

            Assert.That(obj.Nested, Has.Exactly(2).Items);
            Assert.That(obj.Nested[0].Name, Is.EqualTo("one"));
            Assert.That(obj.Nested[0].Dict, Is.Null);

            Assert.That(obj.Nested[1].Name, Is.EqualTo("two"));
            Assert.That(obj.Nested[1].Dict, Has.Exactly(1).Items);
            Assert.That(obj.Nested[1].Dict["three"].Name, Is.EqualTo("three"));
        }

        class TestReadOnly
        {
            public string DontChange { get; } = "  no  ";
        }

        [Test]
        public void ReadOnly()
        {
            var obj = ModelSanitizer.Sanitize(new TestReadOnly());

            Assert.That(obj.DontChange, Is.EqualTo("  no  "));
        }

        class TestWriteOnly
        {
            public string Internal = "  no  ";

            // ReSharper disable once UnusedMember.Local
            public string DontWrite
            {
                set => Internal = value;
            }
        }

        [Test]
        public void WriteOnly()
        {
            var obj = ModelSanitizer.Sanitize(new TestWriteOnly());

            Assert.That(obj.Internal, Is.EqualTo("  no  "));
        }

        [Test]
        public void RealBook()
        {
            var book = new Book
            {
                PrimaryName = "my   book",
                EnglishName = "  another name",
                Category    = (BookCategory) (-3),
                Tags = new Dictionary<BookTag, string[]>
                {
                    [BookTag.Artist]    = new[] { "my   artist", null, "", "my another artist" },
                    [(BookTag) (-10)]   = new[] { "test" },
                    [BookTag.Character] = new string[0]
                },
                Language = LanguageType.French,
                Pages = new[]
                {
                    new BookImage
                    {
                        Hash = new byte[] { 0, 1, 2, 3 }
                    },
                    new BookImage()
                }
            };

            book = ModelSanitizer.Sanitize(book);

            Assert.That(book.PrimaryName, Is.EqualTo("my book"));
            Assert.That(book.EnglishName, Is.EqualTo("another name"));
        }

        [Test]
        public void Dictionary2()
        {
            var dict = new ConcurrentDictionary<string, string>
            {
                [" key "] = " value "
            };

            dict = ModelSanitizer.Sanitize(dict);

            Assert.That(dict, Has.Exactly(1).Items);
            Assert.That(dict.ContainsKey("key"), Is.True);
            Assert.That(dict["key"], Is.EqualTo("value"));
        }

        [Test]
        public void List2()
        {
            var list = new List<string>
            {
                null,
                "",
                " test "
            };

            list = ModelSanitizer.Sanitize(list);

            Assert.That(list, Is.EqualTo(new List<string>
            {
                "test"
            }));
        }

        sealed class CircularReferenceObj
        {
            public string Test { get; set; } = " test ";

            public CircularReferenceObj Reference { get; set; }
        }

        [Test]
        public void CircularReferenceModel()
        {
            var obj = new CircularReferenceObj();

            obj = ModelSanitizer.Sanitize(obj);

            Assert.That(obj.Test, Is.EqualTo("test"));
            Assert.That(obj.Reference, Is.Null);
        }

        /*[Test]
        public void CircularReferenceValue()
        {
            var obj = new CircularReferenceObj();

            obj.Reference = obj;

            obj = ModelSanitizer.Sanitize(obj);

            Assert.That(obj.Test, Is.EqualTo("test"));
            Assert.That(obj.Reference, Is.EqualTo(obj));
        }*/

        [Test]
        public void EmptyDictionary()
        {
            var dict = ModelSanitizer.Sanitize(new Dictionary<string, string>());

            Assert.That(dict, Is.Null);
        }

        [Test]
        public void DictionaryOfStrings()
        {
            var dict = new Dictionary<string, string[]>
            {
                [""]        = new[] { "invalid" },
                [" valid "] = new[] { "  valid ", null, " valid  2" },
                ["invalid"] = new[] { null, "" }
            };

            dict = ModelSanitizer.Sanitize(dict);

            Assert.That(dict, Has.Exactly(1).Items);
            Assert.That(dict, Contains.Key("valid"));
            Assert.That(dict["valid"], Is.EqualTo(new[] { "valid", "valid 2" }));
        }
    }
}