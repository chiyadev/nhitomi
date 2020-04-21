using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace nhitomi.Models.Validation
{
    [ApiController, Route(nameof(ImageAttributeTestController))]
    public class ImageAttributeTestController : ControllerBase
    {
        [HttpPost("valid")]
        public void Valid(ImageAttributeTest.Model _) => Ok();

        [HttpPost("corrupted")]
        public void Corrupted(ImageAttributeTest.Model _) => Assert.Fail("corrupt not detected");

        [HttpPost("dimension")]
        public void Dimension(ImageAttributeTest.ModelWithDimensionLimit _) => Assert.Fail("dimension not validated");
    }

    /// <summary>
    /// <see cref="ImageAttribute"/>
    /// </summary>
    public class ImageAttributeTest : TestBaseHttpClient<ImageAttributeTestController>
    {
        public class Model
        {
            [Required, Image]
            public byte[] Image { get; set; }
        }

        [Test]
        public Task Valid() => PostAsync<object>("valid", new Model
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

                await PostAsync<object>("dimension", new ModelWithDimensionLimit
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