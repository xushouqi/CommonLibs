using System;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;
using CommonLibs;

namespace CommonServices
{
    public class JwtBlackRecord : Entity<int>
    {
        //[Key]
        public string Jti { get; set; }

        public System.DateTime ExpireTime { get; set; }
    }
}
