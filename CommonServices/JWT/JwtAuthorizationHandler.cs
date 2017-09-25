using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using CommonServices.Caching;

namespace CommonServices
{
    public class ValidJtiRequirement : IAuthorizationRequirement
    {
    }

    /// <summary>
    /// 验证token是否在黑名单里
    /// </summary>
    public class JwtAuthorizationHandler : AuthorizationHandler<ValidJtiRequirement>
    {
        private readonly ILogger _logger;
        //private readonly InMemoryCacheClient<JwtBlackRecord> _localCache;
        private readonly RedisClient _redisClient;

        public JwtAuthorizationHandler(ILoggerFactory loggerFactory, RedisClient redisClient)
        {
            _redisClient = redisClient;
            _logger = loggerFactory.CreateLogger<JwtSecurityTokenHandler>();
            //_localCache = new InMemoryCacheClient<JwtBlackRecord>(new InMemoryCacheClientOptions { LoggerFactory = loggerFactory }) { MaxItems = 10000 };
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ValidJtiRequirement requirement)
        {
            // 检查 Jti 是否存在
            var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (jti == null)
            {
                context.Fail(); // 显式的声明验证失败
            }

            // 检查 jti 是否在黑名单
            var tokenExists = await _redisClient.GetDatabase("JWT").KeyExistsAsync(jti);
            if (tokenExists)
            {
                context.Fail();
            }
            else
            {
                context.Succeed(requirement); // 显式的声明验证成功
            }
        }
    }
}
