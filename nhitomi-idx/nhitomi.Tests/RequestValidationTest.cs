using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace nhitomi
{
    [ApiController, Route(nameof(RequestValidationTestController))]
    public  class RequestValidationTestController : ControllerBase
    {
        [HttpGet("path/{str}")]
        public void Path(string str)
            => Assert.That(str, Is.EqualTo("path string"));

        [HttpGet("query")]
        public void Query([FromQuery] string q)
            => Assert.That(q, Is.EqualTo("query string"));

        [HttpPost("form")]
        public void Form([FromForm] string field, [FromForm] string anotherField)
        {
            Assert.That(field, Is.EqualTo("field string"));
            Assert.That(anotherField, Is.EqualTo("another field"));
        }

        [HttpGet("header")]
        public void Header([FromHeader(Name = "x-My-String")] string str)
            => Assert.That(str, Is.EqualTo("header string"));

        [HttpPost("complex")]
        public void Complex(RequestValidationTest.ComplexObject obj)
        {
            Assert.That(obj, Is.Not.Null);
            Assert.That(obj.Good, Is.EqualTo("good"));
            Assert.That(obj.Bad, Is.EqualTo("bad"));

            Assert.That(obj.Nested, Is.Not.Null);
            Assert.That(obj.Nested.Good, Is.EqualTo("good2"));
            Assert.That(obj.Nested.Bad, Is.Null);
            Assert.That(obj.Nested.Nested, Is.Null);

            Assert.That(obj.NestedList, Has.Exactly(1).Items);
            Assert.That(obj.NestedList[0].Good, Is.EqualTo("good3"));
            Assert.That(obj.NestedList[0].Bad, Is.Null);

            Assert.That(obj.EmptyList, Is.Null);
        }

        [HttpPost("required")]
        public void Required(RequestValidationTest.RequiredObj _) =>
            Assert.Fail("Validation not triggered.");
    }

    public class RequestValidationTest : TestBaseHttpClient<RequestValidationTestController>
    {
        [Test]
        public Task Path() => GetAsync<object>("path/    path    string   ");

        [Test]
        public Task Query() => GetAsync<object>("query?q=   query   string   ");

        [Test]
        public Task Form() => RequestAsync(HttpMethod.Post, "form", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["field"]        = "   field  string",
            ["anotherField"] = " another  field   "
        }));

        [Test]
        public Task Header() => RequestAsync(HttpMethod.Get, "header", null, r => r.Headers.TryAddWithoutValidation("x-My-String", "   header  string  "));

        public class ComplexObject
        {
            public string Good { get; set; }
            public string Bad { get; set; }

            public ComplexObject Nested { get; set; }
            public List<ComplexObject> NestedList { get; set; }
            public List<string> EmptyList { get; set; }
        }

        [Test]
        public Task Complex() => PostAsync<object>("complex", new ComplexObject
        {
            Good = "good",
            Bad  = "   bad   ",
            Nested = new ComplexObject
            {
                Good = "good2",
                Bad  = null
            },
            NestedList = new List<ComplexObject>
            {
                null,
                new ComplexObject
                {
                    Good = "good3"
                },
                null
            },
            EmptyList = new List<string>()
        });

        public class RequiredObj
        {
            [Required]
            public string Str { get; set; }
        }

        [Test]
        public async Task Required()
        {
            try
            {
                await PostAsync<object>("required", new RequiredObj());
            }
            catch (RequestException e)
            {
                Assert.That(e.Response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
            }
        }

        [Test]
        public async Task RequiredWithSanitization()
        {
            try
            {
                await PostAsync<object>("required", new RequiredObj
                {
                    Str = "       "
                });
            }
            catch (RequestException e)
            {
                Assert.That(e.Response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
            }
        }
    }
}