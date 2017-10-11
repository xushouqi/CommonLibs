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
	[AuthPolicy(AuthPolicy = UserTypeEnum.Admin)]
    public class Action1011 : ActionBase<bool>
    {
        private readonly UserService m_service;

        public Action1011(
            UserService service)
        {
            m_service = service;
            m_actionId = 1011;
        }
        
        public override async Task DoAction()
        {
            if (m_params != null)
            {
                var accountid = m_params.ReadInt();
                var mobile = m_params.ReadString();

                var retData = await m_service.ModifyMobile(accountid, mobile);
				var data = retData;

                m_return = data;
            }
            await base.DoAction();
        }
    }
}
