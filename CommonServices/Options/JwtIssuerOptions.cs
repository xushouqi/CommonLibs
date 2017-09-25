using System;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace CommonServices
{
    public class JwtIssuerOptions
    {
        /// <summary>
        /// key
        /// </summary>
        public string SecretKey { get; set; }
        /// <summary>
        /// iss - Issuer 代表这个jwt的签发主体
        /// </summary>
        /// <remarks></remarks>
        public string Issuer { get; set; }
        /// <summary>
        /// sub - Subject 代表这个jwt的主体,即它的所有人
        /// </summary>
        /// <remarks>表示  --> 发起请求的用户, 直接看控制器中返回的数据</remarks>
        public string Subject { get; set; }
        /// <summary>
        /// aud - Audience 代表这个jwt的接收对象
        /// </summary>
        public string Audience { get; set; }
        /// <summary>
        /// nbf - Not Before 时间戳,代表这个jwt的生效时间, 即在此之前是无效的
        /// </summary>
        public DateTime NotBefore => DateTime.UtcNow;
        /// <summary>
        /// iat - Issue At 时间戳, 代表这个jwt的签发时间
        /// </summary>
        public DateTime IssueAt => DateTime.Now;
        /// <summary>
        /// 有效时间（单位秒）
        /// </summary>
        public double ValidForSeconds { get; set; } = 60 * 60 * 24 * 7;
        public TimeSpan ValidFor => TimeSpan.FromSeconds(ValidForSeconds);
        /// <summary>
        /// exp - Expiration 时间戳,代表这个jwt的过期时间
        /// </summary>
        public DateTime Expiration => IssueAt.Add(ValidFor);
        /// <summary>
        /// Func委托方法(对象), 生成 jti - JWT ID 唯一标识
        /// </summary>
        public Func<Task<string>> JtiGenerator => () => Task.FromResult(Guid.NewGuid().ToString());
        /// <summary>
        /// 生成token时使用的签名
        /// </summary>
        public SigningCredentials SigningCredentials { get; set; }
    }
}
