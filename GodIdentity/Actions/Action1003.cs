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
    public class Action1003 : ActionBase<bool>
    {
        private readonly UserService m_service;

        public Action1003(
            UserService service)
        {
            m_service = service;
            m_actionId = 1003;
        }
        
        public override async Task DoAction()
        {
            if (m_params != null)
            {
                var username = m_params.ReadString();
                var oldpassword = m_params.ReadString();
                var password = m_params.ReadString();

                var retData = await m_service.ChangePassword(username, oldpassword, password);
				var data = retData;

                m_return = data;
            }
            await base.DoAction();
        }
    }
}
