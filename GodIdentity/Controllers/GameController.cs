using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CommonLibs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Cors;
using AutoMapper;
using Microsoft.Extensions.Logging;
using GodModels;
using GodModels.ViewModels;

namespace GodIdentity.Controllers
{
    [EnableCors("AllowSameDomain")]
    [ServiceFilter(typeof(ApiExceptionFilter))]
    [Produces("application/json")]
    [Route("api/[controller]/[action]")]
    public class GameController : Controller
    {
        private readonly GodIdentity.Services.GameService _actionService;
        private readonly ILogger _logger;

        public GameController(ILoggerFactory logFactory, 
            GodIdentity.Services.GameService actionService)
        {
            _actionService = actionService;
            _logger = logFactory.CreateLogger("Error");
        }
		
        private int GetCurrentAccountId()
        {
            int accountid = -1;
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier).Value, out accountid);
            return accountid;
        }


        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> TryConnectIdentity(string GSID, int userCount, string sign)
        {
            try
            {
				string design = string.Empty;
				if (!string.IsNullOrEmpty(sign))
				{
					design = RsaService.DecryptToString(sign, "GS");
				}
				else if (Request.ContentLength != null)
				{
					byte[] datas = new byte[(int)Request.ContentLength];
					var ret = Request.Body.Read(datas, 0, (int)Request.ContentLength);
					design = RsaService.DecryptToString(datas, "GS");
				}
				if (!string.IsNullOrEmpty(design))
				{
					var tmp = Common.QueryStringToData(design);
					if (tmp != null && tmp.ContainsKey("GSID") && tmp.ContainsKey("userCount"))
					{
						var retData = await _actionService.TryConnectIdentity(tmp["GSID"], int.Parse(tmp["userCount"]));
						var data = retData;

						if (data != null)
						{
							return new OkObjectResult(data);
						}
						else
							return NoContent();
					}
					else
						return new UnauthorizedResult();
				}
				else
				{
					var retData = await _actionService.TryConnectIdentity(GSID, userCount);
					var data = retData;

					if (data != null)
					{
						return new OkObjectResult(data);
					}
					else
						return NoContent();
				}
            }
            catch(Exception e)
            {
                _logger.LogError(string.Format("Exception={0}\n ExceptionSource={1}\n StackTrace={2}",
                    e.Message, e.Source, e.StackTrace));
                return new BadRequestResult();
            }
        }

		
    }
}
