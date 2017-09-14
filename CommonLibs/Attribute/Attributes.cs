using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonLibs
{
    public enum UserTypeEnum
    {
        None = 0,
        Member = 1,
        Admin = 2,
        SuperAdmin = 4,
    }
    //public enum AuthIDTypeEnum
    //{
    //    None = 0,
    //    AccountId,
    //    RoleId,
    //    TeamId,
    //}

    [AttributeUsage(AttributeTargets.Method)]
    public class ApiAttribute : System.Attribute
    {
        public int ActionId;
        public Type ReturnType = null;
        //public bool IsGet = false;
        //public bool Encrypt = false;
        //public bool IsValidToken = false;
        public bool RegPushData = false;
        public UserTypeEnum AuthPolicy = UserTypeEnum.None;
        //public AuthIDTypeEnum AuthIDType = AuthIDTypeEnum.None;
        public string Tips;

    }

    [AttributeUsage(AttributeTargets.Class)]
    public class WebApiAttribute : System.Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class WebSocketAttribute : System.Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DataModelsAttribute : System.Attribute
    {
    }

    //[AttributeUsage(AttributeTargets.Class)]
    //public class TryLoginAttribute : System.Attribute
    //{
    //}

    [AttributeUsage(AttributeTargets.Class)]
    public class AuthPolicyAttribute : System.Attribute
    {
        public UserTypeEnum AuthPolicy = UserTypeEnum.None;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DataViewAttribute : System.Attribute
    {
        public string Tips = string.Empty;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class XslToDbAttribute : System.Attribute
    {
    }
}
