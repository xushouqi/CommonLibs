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
    public class UserController : Controller
    {
        private readonly GodIdentity.Services.UserService _actionService;
        private readonly ILogger _logger;

        public UserController(ILoggerFactory logFactory, 
            GodIdentity.Services.UserService actionService)
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
        [Authorize(Policy = "SuperAdmin")]
        public async Task<IActionResult> CreateAccountByType(string username, string password, CommonLibs.UserTypeEnum atype, string sign)
        {
            try
            {
				string design = string.Empty;
				if (!string.IsNullOrEmpty(sign))
				{
					design = RsaService.DecryptToString(sign, "");
				}
				else if (Request.ContentLength != null)
				{
					byte[] datas = new byte[(int)Request.ContentLength];
					var ret = Request.Body.Read(datas, 0, (int)Request.ContentLength);
					design = RsaService.DecryptToString(datas, "");
				}
				if (!string.IsNullOrEmpty(design))
				{
					var tmp = Common.QueryStringToData(design);
					if (tmp != null && tmp.ContainsKey("username") && tmp.ContainsKey("password") && tmp.ContainsKey("atype"))
					{
						var retData = await _actionService.CreateAccountByType(tmp["username"], tmp["password"], (CommonLibs.UserTypeEnum)(int.Parse(tmp["atype"])));
						var data = new ReturnData<AccountData>{
                    ErrorCode = retData.ErrorCode,
                    Data = Mapper.Map<AccountData>(retData.Data),
                };

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
					var retData = await _actionService.CreateAccountByType(username, password, atype);
					var data = new ReturnData<AccountData>{
                    ErrorCode = retData.ErrorCode,
                    Data = Mapper.Map<AccountData>(retData.Data),
                };

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

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(string username, string password, string sign)
        {
            try
            {
				string design = string.Empty;
				if (!string.IsNullOrEmpty(sign))
				{
					design = RsaService.DecryptToString(sign, "");
				}
				else if (Request.ContentLength != null)
				{
					byte[] datas = new byte[(int)Request.ContentLength];
					var ret = Request.Body.Read(datas, 0, (int)Request.ContentLength);
					design = RsaService.DecryptToString(datas, "");
				}
				if (!string.IsNullOrEmpty(design))
				{
					var tmp = Common.QueryStringToData(design);
					if (tmp != null && tmp.ContainsKey("username") && tmp.ContainsKey("password"))
					{
						var retData = await _actionService.Register(tmp["username"], tmp["password"]);
						var data = new ReturnData<AccountData>{
                    ErrorCode = retData.ErrorCode,
                    Data = Mapper.Map<AccountData>(retData.Data),
                };

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
					var retData = await _actionService.Register(username, password);
					var data = new ReturnData<AccountData>{
                    ErrorCode = retData.ErrorCode,
                    Data = Mapper.Map<AccountData>(retData.Data),
                };

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

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ChangePassword(string username, string oldpassword, string password, string sign)
        {
            try
            {
				string design = string.Empty;
				if (!string.IsNullOrEmpty(sign))
				{
					design = RsaService.DecryptToString(sign, "");
				}
				else if (Request.ContentLength != null)
				{
					byte[] datas = new byte[(int)Request.ContentLength];
					var ret = Request.Body.Read(datas, 0, (int)Request.ContentLength);
					design = RsaService.DecryptToString(datas, "");
				}
				if (!string.IsNullOrEmpty(design))
				{
					var tmp = Common.QueryStringToData(design);
					if (tmp != null && tmp.ContainsKey("username") && tmp.ContainsKey("oldpassword") && tmp.ContainsKey("password"))
					{
						var retData = await _actionService.ChangePassword(tmp["username"], tmp["oldpassword"], tmp["password"]);
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
					var retData = await _actionService.ChangePassword(username, oldpassword, password);
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

        [HttpPost]
        [Authorize(Policy = "SuperAdmin")]
        public async Task<IActionResult> ChangePasswordByAdmin(string username, string password, string sign)
        {
            try
            {
				string design = string.Empty;
				if (!string.IsNullOrEmpty(sign))
				{
					design = RsaService.DecryptToString(sign, "");
				}
				else if (Request.ContentLength != null)
				{
					byte[] datas = new byte[(int)Request.ContentLength];
					var ret = Request.Body.Read(datas, 0, (int)Request.ContentLength);
					design = RsaService.DecryptToString(datas, "");
				}
				if (!string.IsNullOrEmpty(design))
				{
					var tmp = Common.QueryStringToData(design);
					if (tmp != null && tmp.ContainsKey("username") && tmp.ContainsKey("password"))
					{
						var retData = await _actionService.ChangePasswordByAdmin(tmp["username"], tmp["password"]);
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
					var retData = await _actionService.ChangePasswordByAdmin(username, password);
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

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password, string sign)
        {
            try
            {
				string design = string.Empty;
				if (!string.IsNullOrEmpty(sign))
				{
					design = RsaService.DecryptToString(sign, "");
				}
				else if (Request.ContentLength != null)
				{
					byte[] datas = new byte[(int)Request.ContentLength];
					var ret = Request.Body.Read(datas, 0, (int)Request.ContentLength);
					design = RsaService.DecryptToString(datas, "");
				}
				if (!string.IsNullOrEmpty(design))
				{
					var tmp = Common.QueryStringToData(design);
					if (tmp != null && tmp.ContainsKey("username") && tmp.ContainsKey("password"))
					{
						var retData = await _actionService.Login(tmp["username"], tmp["password"]);
						var data = new ReturnData<AccountData>{
                    ErrorCode = retData.ErrorCode,
                    Data = Mapper.Map<AccountData>(retData.Data),
                };

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
					var retData = await _actionService.Login(username, password);
					var data = new ReturnData<AccountData>{
                    ErrorCode = retData.ErrorCode,
                    Data = Mapper.Map<AccountData>(retData.Data),
                };

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

        [HttpPost]
        [Authorize(Policy = "Member")]
        public async Task<IActionResult> UpdateMyToken(string sign)
        {
            try
            {
				string design = string.Empty;
				if (!string.IsNullOrEmpty(sign))
				{
					design = RsaService.DecryptToString(sign, "");
				}
				else if (Request.ContentLength != null)
				{
					byte[] datas = new byte[(int)Request.ContentLength];
					var ret = Request.Body.Read(datas, 0, (int)Request.ContentLength);
					design = RsaService.DecryptToString(datas, "");
				}
				if (!string.IsNullOrEmpty(design))
				{
					var tmp = Common.QueryStringToData(design);
					if (tmp != null)
					{
						var accountId = GetCurrentAccountId();
						if (accountId > 0)
						{
							var retData = await _actionService.UpdateMyToken(accountId);
							var data = new ReturnData<AccountData>{
                    ErrorCode = retData.ErrorCode,
                    Data = Mapper.Map<AccountData>(retData.Data),
                };

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
						return new UnauthorizedResult();
				}
				else
				{
					var accountId = GetCurrentAccountId();
					if (accountId > 0)
					{
						var retData = await _actionService.UpdateMyToken(accountId);
						var data = new ReturnData<AccountData>{
                    ErrorCode = retData.ErrorCode,
                    Data = Mapper.Map<AccountData>(retData.Data),
                };

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
            }
            catch(Exception e)
            {
                _logger.LogError(string.Format("Exception={0}\n ExceptionSource={1}\n StackTrace={2}",
                    e.Message, e.Source, e.StackTrace));
                return new BadRequestResult();
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ValidAccountByToken(string token, string sign)
        {
            try
            {
				string design = string.Empty;
				if (!string.IsNullOrEmpty(sign))
				{
					design = RsaService.DecryptToString(sign, "");
				}
				else if (Request.ContentLength != null)
				{
					byte[] datas = new byte[(int)Request.ContentLength];
					var ret = Request.Body.Read(datas, 0, (int)Request.ContentLength);
					design = RsaService.DecryptToString(datas, "");
				}
				if (!string.IsNullOrEmpty(design))
				{
					var tmp = Common.QueryStringToData(design);
					if (tmp != null && tmp.ContainsKey("token"))
					{
						var retData = await _actionService.ValidAccountByToken(tmp["token"]);
						var data = new ReturnData<AccountData>{
                    ErrorCode = retData.ErrorCode,
                    Data = Mapper.Map<AccountData>(retData.Data),
                };

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
					var retData = await _actionService.ValidAccountByToken(token);
					var data = new ReturnData<AccountData>{
                    ErrorCode = retData.ErrorCode,
                    Data = Mapper.Map<AccountData>(retData.Data),
                };

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

        [HttpPost]
        [Authorize(Policy = "Member")]
        public async Task<IActionResult> Logout(string sign)
        {
            try
            {
				string design = string.Empty;
				if (!string.IsNullOrEmpty(sign))
				{
					design = RsaService.DecryptToString(sign, "");
				}
				else if (Request.ContentLength != null)
				{
					byte[] datas = new byte[(int)Request.ContentLength];
					var ret = Request.Body.Read(datas, 0, (int)Request.ContentLength);
					design = RsaService.DecryptToString(datas, "");
				}
				if (!string.IsNullOrEmpty(design))
				{
					var tmp = Common.QueryStringToData(design);
					if (tmp != null)
					{
						var accountId = GetCurrentAccountId();
						if (accountId > 0)
						{
							var retData = await _actionService.Logout(accountId);
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
						return new UnauthorizedResult();
				}
				else
				{
					var accountId = GetCurrentAccountId();
					if (accountId > 0)
					{
						var retData = await _actionService.Logout(accountId);
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
            }
            catch(Exception e)
            {
                _logger.LogError(string.Format("Exception={0}\n ExceptionSource={1}\n StackTrace={2}",
                    e.Message, e.Source, e.StackTrace));
                return new BadRequestResult();
            }
        }

        [HttpPost]
        [Authorize(Policy = "Member")]
        public async Task<IActionResult> ModifyIDCode(string name, string idcode, string sign)
        {
            try
            {
				string design = string.Empty;
				if (!string.IsNullOrEmpty(sign))
				{
					design = RsaService.DecryptToString(sign, "");
				}
				else if (Request.ContentLength != null)
				{
					byte[] datas = new byte[(int)Request.ContentLength];
					var ret = Request.Body.Read(datas, 0, (int)Request.ContentLength);
					design = RsaService.DecryptToString(datas, "");
				}
				if (!string.IsNullOrEmpty(design))
				{
					var tmp = Common.QueryStringToData(design);
					if (tmp != null && tmp.ContainsKey("name") && tmp.ContainsKey("idcode"))
					{
						var accountId = GetCurrentAccountId();
						if (accountId > 0)
						{
							var retData = await _actionService.ModifyIDCode(accountId, tmp["name"], tmp["idcode"]);
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
						return new UnauthorizedResult();
				}
				else
				{
					var accountId = GetCurrentAccountId();
					if (accountId > 0)
					{
						var retData = await _actionService.ModifyIDCode(accountId, name, idcode);
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
            }
            catch(Exception e)
            {
                _logger.LogError(string.Format("Exception={0}\n ExceptionSource={1}\n StackTrace={2}",
                    e.Message, e.Source, e.StackTrace));
                return new BadRequestResult();
            }
        }

        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> ModifyMobile(int accountid, string mobile, string sign)
        {
            try
            {
				string design = string.Empty;
				if (!string.IsNullOrEmpty(sign))
				{
					design = RsaService.DecryptToString(sign, "");
				}
				else if (Request.ContentLength != null)
				{
					byte[] datas = new byte[(int)Request.ContentLength];
					var ret = Request.Body.Read(datas, 0, (int)Request.ContentLength);
					design = RsaService.DecryptToString(datas, "");
				}
				if (!string.IsNullOrEmpty(design))
				{
					var tmp = Common.QueryStringToData(design);
					if (tmp != null && tmp.ContainsKey("accountid") && tmp.ContainsKey("mobile"))
					{
						var retData = await _actionService.ModifyMobile(int.Parse(tmp["accountid"]), tmp["mobile"]);
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
					var retData = await _actionService.ModifyMobile(accountid, mobile);
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

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GetMobile(string username, string sign)
        {
            try
            {
				string design = string.Empty;
				if (!string.IsNullOrEmpty(sign))
				{
					design = RsaService.DecryptToString(sign, "");
				}
				else if (Request.ContentLength != null)
				{
					byte[] datas = new byte[(int)Request.ContentLength];
					var ret = Request.Body.Read(datas, 0, (int)Request.ContentLength);
					design = RsaService.DecryptToString(datas, "");
				}
				if (!string.IsNullOrEmpty(design))
				{
					var tmp = Common.QueryStringToData(design);
					if (tmp != null && tmp.ContainsKey("username"))
					{
						var retData = await _actionService.GetMobile(tmp["username"]);
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
					var retData = await _actionService.GetMobile(username);
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

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CheckValidIDCode(string idcode, string sign)
        {
            try
            {
				string design = string.Empty;
				if (!string.IsNullOrEmpty(sign))
				{
					design = RsaService.DecryptToString(sign, "");
				}
				else if (Request.ContentLength != null)
				{
					byte[] datas = new byte[(int)Request.ContentLength];
					var ret = Request.Body.Read(datas, 0, (int)Request.ContentLength);
					design = RsaService.DecryptToString(datas, "");
				}
				if (!string.IsNullOrEmpty(design))
				{
					var tmp = Common.QueryStringToData(design);
					if (tmp != null && tmp.ContainsKey("idcode"))
					{
						var retData = await _actionService.CheckValidIDCode(tmp["idcode"]);
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
					var retData = await _actionService.CheckValidIDCode(idcode);
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

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GetAccountByUsername(string username, string sign)
        {
            try
            {
				string design = string.Empty;
				if (!string.IsNullOrEmpty(sign))
				{
					design = RsaService.DecryptToString(sign, "");
				}
				else if (Request.ContentLength != null)
				{
					byte[] datas = new byte[(int)Request.ContentLength];
					var ret = Request.Body.Read(datas, 0, (int)Request.ContentLength);
					design = RsaService.DecryptToString(datas, "");
				}
				if (!string.IsNullOrEmpty(design))
				{
					var tmp = Common.QueryStringToData(design);
					if (tmp != null && tmp.ContainsKey("username"))
					{
						var retData = await _actionService.GetAccountByUsername(tmp["username"]);
						var data = new ReturnData<AccountData>{
                    ErrorCode = retData.ErrorCode,
                    Data = Mapper.Map<AccountData>(retData.Data),
                };

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
					var retData = await _actionService.GetAccountByUsername(username);
					var data = new ReturnData<AccountData>{
                    ErrorCode = retData.ErrorCode,
                    Data = Mapper.Map<AccountData>(retData.Data),
                };

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

        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetAccountById(int id, string sign)
        {
            try
            {
				string design = string.Empty;
				if (!string.IsNullOrEmpty(sign))
				{
					design = RsaService.DecryptToString(sign, "");
				}
				else if (Request.ContentLength != null)
				{
					byte[] datas = new byte[(int)Request.ContentLength];
					var ret = Request.Body.Read(datas, 0, (int)Request.ContentLength);
					design = RsaService.DecryptToString(datas, "");
				}
				if (!string.IsNullOrEmpty(design))
				{
					var tmp = Common.QueryStringToData(design);
					if (tmp != null && tmp.ContainsKey("id"))
					{
						var retData = await _actionService.GetAccountById(int.Parse(tmp["id"]));
						var data = new ReturnData<AccountData>{
                    ErrorCode = retData.ErrorCode,
                    Data = Mapper.Map<AccountData>(retData.Data),
                };

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
					var retData = await _actionService.GetAccountById(id);
					var data = new ReturnData<AccountData>{
                    ErrorCode = retData.ErrorCode,
                    Data = Mapper.Map<AccountData>(retData.Data),
                };

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

        [HttpPost]
        [Authorize(Policy = "Member")]
        public async Task<IActionResult> GetCurrentServer(string sign)
        {
            try
            {
				string design = string.Empty;
				if (!string.IsNullOrEmpty(sign))
				{
					design = RsaService.DecryptToString(sign, "");
				}
				else if (Request.ContentLength != null)
				{
					byte[] datas = new byte[(int)Request.ContentLength];
					var ret = Request.Body.Read(datas, 0, (int)Request.ContentLength);
					design = RsaService.DecryptToString(datas, "");
				}
				if (!string.IsNullOrEmpty(design))
				{
					var tmp = Common.QueryStringToData(design);
					if (tmp != null)
					{
						var accountId = GetCurrentAccountId();
						if (accountId > 0)
						{
							var retData = await _actionService.GetCurrentServer();
							var data = new ReturnData<ServerData>{
                    ErrorCode = retData.ErrorCode,
                    Data = Mapper.Map<ServerData>(retData.Data),
                };

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
						return new UnauthorizedResult();
				}
				else
				{
					var accountId = GetCurrentAccountId();
					if (accountId > 0)
					{
						var retData = await _actionService.GetCurrentServer();
						var data = new ReturnData<ServerData>{
                    ErrorCode = retData.ErrorCode,
                    Data = Mapper.Map<ServerData>(retData.Data),
                };

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
            }
            catch(Exception e)
            {
                _logger.LogError(string.Format("Exception={0}\n ExceptionSource={1}\n StackTrace={2}",
                    e.Message, e.Source, e.StackTrace));
                return new BadRequestResult();
            }
        }

        [HttpPost]
        [Authorize(Policy = "Member")]
        public async Task<IActionResult> GetTOTPCode(string sign)
        {
            try
            {
				string design = string.Empty;
				if (!string.IsNullOrEmpty(sign))
				{
					design = RsaService.DecryptToString(sign, "");
				}
				else if (Request.ContentLength != null)
				{
					byte[] datas = new byte[(int)Request.ContentLength];
					var ret = Request.Body.Read(datas, 0, (int)Request.ContentLength);
					design = RsaService.DecryptToString(datas, "");
				}
				if (!string.IsNullOrEmpty(design))
				{
					var tmp = Common.QueryStringToData(design);
					if (tmp != null)
					{
						var accountId = GetCurrentAccountId();
						if (accountId > 0)
						{
							var retData = await _actionService.GetTOTPCode(accountId);
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
						return new UnauthorizedResult();
				}
				else
				{
					var accountId = GetCurrentAccountId();
					if (accountId > 0)
					{
						var retData = await _actionService.GetTOTPCode(accountId);
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
            }
            catch(Exception e)
            {
                _logger.LogError(string.Format("Exception={0}\n ExceptionSource={1}\n StackTrace={2}",
                    e.Message, e.Source, e.StackTrace));
                return new BadRequestResult();
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CheckTOTPCode(string code, string sign)
        {
            try
            {
				string design = string.Empty;
				if (!string.IsNullOrEmpty(sign))
				{
					design = RsaService.DecryptToString(sign, "");
				}
				else if (Request.ContentLength != null)
				{
					byte[] datas = new byte[(int)Request.ContentLength];
					var ret = Request.Body.Read(datas, 0, (int)Request.ContentLength);
					design = RsaService.DecryptToString(datas, "");
				}
				if (!string.IsNullOrEmpty(design))
				{
					var tmp = Common.QueryStringToData(design);
					if (tmp != null && tmp.ContainsKey("code"))
					{
						var retData = await _actionService.CheckTOTPCode(tmp["code"]);
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
					var retData = await _actionService.CheckTOTPCode(code);
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
