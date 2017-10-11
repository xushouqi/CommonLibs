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
	[AuthPolicy(AuthPolicy = UserTypeEnum.SuperAdmin)]
    public class Action1001 : ActionBase<AccountData>
    {
        private readonly UserService m_service;

        public Action1001(
            UserService service)
        {
            m_service = service;
            m_actionId = 1001;
        }
        
        public override async Task DoAction()
        {
            if (m_params != null)
            {
                var username = m_params.ReadString();
                var password = m_params.ReadString();
                var atype = (CommonLibs.UserTypeEnum)m_params.ReadInt();

                var retData = await m_service.CreateAccountByType(username, password, atype);
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
