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
    public class Action1009 : ActionBase<bool>
    {
        private readonly UserService m_service;

        public Action1009(
            UserService service)
        {
            m_service = service;
            m_actionId = 1009;
        }
        
        public override async Task DoAction()
        {
            if (m_params != null)
            {

                var retData = await m_service.Logout(m_accountId);
				var data = retData;

                m_return = data;
            }
            await base.DoAction();
        }
    }
}
