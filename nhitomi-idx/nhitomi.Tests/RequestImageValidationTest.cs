using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using nhitomi.Models.Validation;
using NUnit.Framework;

namespace nhitomi
{
    [ApiController, Route(nameof(RequestImageValidationTestController))]
    public class RequestImageValidationTestController : ControllerBase
    {
        [HttpPost("valid")]
        public void Valid(RequestImageValidationTest.Model model) => Ok();

        [HttpPost("corrupted")]
        public void Corrupted(RequestImageValidationTest.Model model) => Assert.Fail("corrupt not detected");

        [HttpPost("dimension")]
        public void Dimension(RequestImageValidationTest.ModelWithDimensionLimit model) => Assert.Fail("corrupt not detected");
    }

    public class RequestImageValidationTest : TestBaseHttpClient
    {
        protected override string RequestPathPrefix => base.RequestPathPrefix + nameof(RequestImageValidationTestController);

        public class Model
        {
            [Required, Image]
            public byte[] Image { get; set; }
        }

        [Test]
        public Task Valid()
            => PostAsync<object>("valid", new Model
            {
                Image = TestUtils.DummyImage()
            });

        [Test]
        public async Task Corrupted()
        {
            try
            {
                var buffer = TestUtils.DummyImage();

                for (var i = 10; i < buffer.Length; i++)
                    buffer[i] = unchecked((byte) i);

                await PostAsync<object>("corrupted", new Model
                {
                    Image = buffer
                });
            }
            catch (RequestException e)
            {
                Assert.That(e.Response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
            }
        }

        public class ModelWithDimensionLimit
        {
            [Required, Image(MaxWidth = 500, MinHeight = 200)]
            public byte[] Image { get; set; }
        }

        [Test]
        public async Task Dimensions()
        {
            try
            {
                var buffer = TestUtils.DummyImage(400, 500);

                for (var i = 10; i < buffer.Length; i++)
                    buffer[i] = unchecked((byte) i);

                await PostAsync<object>("dimension", new Model
                {
                    Image = buffer
                });
            }
            catch (RequestException e)
            {
                Assert.That(e.Response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
            }
        }
    }
}