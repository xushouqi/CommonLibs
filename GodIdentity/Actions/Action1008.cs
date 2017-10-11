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
    public class Action1008 : ActionBase<AccountData>
    {
        private readonly UserService m_service;

        public Action1008(
            UserService service)
        {
            m_service = service;
            m_actionId = 1008;
        }
        
        public override async Task DoAction()
        {
            if (m_params != null)
            {
                var token = m_params.ReadString();

                var retData = await m_service.ValidAccountByToken(token);
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
