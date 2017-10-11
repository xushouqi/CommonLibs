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
	[AuthPolicy(AuthPolicy = UserTypeEnum.None)]
    public class Action1014 : ActionBase<AccountData>
    {
        private readonly UserService m_service;

        public Action1014(
            UserService service)
        {
            m_service = service;
            m_actionId = 1014;
        }
        
        public override async Task DoAction()
        {
            if (m_params != null)
            {
                var username = m_params.ReadString();

                var retData = await m_service.GetAccountByUsername(username);
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
