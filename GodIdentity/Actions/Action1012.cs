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
    public class Action1012 : ActionBase<string>
    {
        private readonly UserService m_service;

        public Action1012(
            UserService service)
        {
            m_service = service;
            m_actionId = 1012;
        }
        
        public override async Task DoAction()
        {
            if (m_params != null)
            {
                var username = m_params.ReadString();

                var retData = await m_service.GetMobile(username);
				var data = retData;

                m_return = data;
            }
            await base.DoAction();
        }
    }
}
