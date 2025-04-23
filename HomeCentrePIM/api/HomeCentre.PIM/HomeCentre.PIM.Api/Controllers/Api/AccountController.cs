using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.Account;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.CommonWeb;
using Microsoft.AspNetCore.Mvc;

namespace HomeCentre.PIM.Api.Controllers.Api
{
    [Route("/api/[controller]/[action]")]
    public class AccountController : BaseApiController
    {
        private readonly IUserManager _userManager;
        private readonly IUserContext _context;
        private readonly IBlobRepo _blobRepo;

        public AccountController(IUserManager userManager, IUserContext context, IBlobRepo blobRepo)
        {
            _userManager = userManager;
            _context = context;
            _blobRepo = blobRepo;
        }

        [HttpPost]
        public async Task<ActionResult<string>> Authenticate([FromBody] LoginViewModel model)
        {
            model.IpAddress = this._context.GetIpAddress();
            return await _userManager.Authenticate(model);
        }

        [HttpGet]
        public async Task<MeModel> Me()
        {
            return await _userManager.GetCurrentUser();
        }
    }
}