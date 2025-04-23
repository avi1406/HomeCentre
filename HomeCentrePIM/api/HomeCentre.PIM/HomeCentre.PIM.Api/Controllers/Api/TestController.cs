using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.CommonWeb;
using Microsoft.AspNetCore.Mvc;

namespace HomeCentre.PIM.Api.Controllers.Api
{
    [Route("/api/[controller]/[action]")]
    [MultiplePoliciesAuthorize(Constants.Policies.All)]
    public class TestController : Controller
    {
        private readonly IMailEngine _engine;

        public TestController(IMailEngine engine)
        {
            _engine = engine;
        }

        [HttpGet]
        public async Task SendEmail(string email)
        {
            await _engine.SendEmail("TestEmailTemplate", null, "Test From BIF", null, new[] { email });
        }
    }
}