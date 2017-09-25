using System;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;
using CommonLibs;

namespace CommonServices
{
    public class JwtBlackRecord : Entity
    {
        public override int GetId()
        {
            return 0;
        }
        public override string GetKey()
        {
            return Jti;
        }
        public override DateTime TryUpdateTime()
        {
            return DateTime.Now;
        }

        //[Key]
        public string Jti { get; set; }

        public System.DateTime ExpireTime { get; set; }
    }
}
