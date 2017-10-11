using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GodIdentity.Services;
using CommonLibs;
using CommonNetwork;
using AutoMapper;
using GodModels;
using GodModels.ViewModels;

namespace GodIdentity.Actions
{
	[AuthPolicy(AuthPolicy = UserTypeEnum.Member)]
    public class Action1007 : ActionBase<AccountData>
    {
        private readonly UserService m_service;

        public Action1007(
            UserService service)
        {
            m_service = service;
            m_actionId = 1007;
        }
        
        public override async Task DoAction()
        {
            if (m_params != null)
            {

                var retData = await m_service.UpdateMyToken(m_accountId, m_socket);
				var data = new ReturnData<AccountData>{
                    ErrorCode = retData.ErrorCode,
                    Data = Mapper.Map<AccountData>(retData.Data),
                };

                m_return = data;
            }
            await base.DoAction();
        }
    }
}
