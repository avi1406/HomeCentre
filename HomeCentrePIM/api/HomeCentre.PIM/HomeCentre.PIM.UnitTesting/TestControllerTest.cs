using HomeCentre.PIM.Api.Controllers.Api;
using HomeCentre.PIM.BAL.Contracts;
using Moq;
using RazorEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.UnitTesting
{
    public class TestControllerTest
    {
        private IMailEngine _engine;

        [SetUp]
        public void Setup()
        {
            _engine = Mock.Of<IMailEngine>();
        }
        [Test]
        public async Task SendEmail_Test()
        {
            string email = "nagarjun.m@joyitsolutions.co";
            var emailController = new TestController(_engine);
            await emailController.SendEmail(email);
            Mock.Get(_engine).Verify(
                engine => engine.SendEmail("TestEmailTemplate", null, "Test From BIF", null, new[] { email }, null, null),
                Times.Once);
            Assert.Pass();
        }
    }
}
