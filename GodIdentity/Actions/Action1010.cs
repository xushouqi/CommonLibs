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
    public class Action1010 : ActionBase<bool>
    {
        private readonly UserService m_service;

        public Action1010(
            UserService service)
        {
            m_service = service;
            m_actionId = 1010;
        }
        
        public override async Task DoAction()
        {
            if (m_params != null)
            {
                var name = m_params.ReadString();
                var idcode = m_params.ReadString();

                var retData = await m_service.ModifyIDCode(m_accountId, name, idcode);
				var data = retData;

                m_return = data;
            }
            await base.DoAction();
        }
    }
}
