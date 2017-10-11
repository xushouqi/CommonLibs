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
    public class Action1018 : ActionBase<string>
    {
        private readonly UserService m_service;

        public Action1018(
            UserService service)
        {
            m_service = service;
            m_actionId = 1018;
        }
        
        public override async Task DoAction()
        {
            if (m_params != null)
            {
                var code = m_params.ReadString();

                var retData = await m_service.CheckTOTPCode(code);
				var data = retData;

                m_return = data;
            }
            await base.DoAction();
        }
    }
}
