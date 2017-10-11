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
    public class Action1016 : ActionBase<ServerData>
    {
        private readonly UserService m_service;

        public Action1016(
            UserService service)
        {
            m_service = service;
            m_actionId = 1016;
        }
        
        public override async Task DoAction()
        {
            if (m_params != null)
            {

                var retData = await m_service.GetCurrentServer();
				var data = new ReturnData<ServerData>{
                    ErrorCode = retData.ErrorCode,
                    Data = Mapper.Map<ServerData>(retData.Data),
                };

                m_return = data;
            }
            await base.DoAction();
        }
    }
}
