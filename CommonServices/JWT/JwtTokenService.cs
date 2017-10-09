using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CommonLibs;

namespace CommonServices
{
    public class JwtTokenService
    {
        private readonly JwtIssuerOptions _jwtOptions;
        private readonly ILogger _logger;

        public JwtTokenService(ILogger<JwtTokenService> logService,
                            IOptions<JwtIssuerOptions> jwtoptions)
        {
            _logger = logService;

            _jwtOptions = jwtoptions.Value;
            ThrowIfInvalidOptions(_jwtOptions);
        }

        /// <summary>
        /// 验证jwt配置是否合法
        /// </summary>
        /// <param name="options">配置对象</param>
        private static void ThrowIfInvalidOptions(JwtIssuerOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (options.ValidFor <= TimeSpan.Zero)
                throw new ArgumentException("Must be a non-zero TimeSpan", nameof(JwtIssuerOptions.ValidFor));
            if (options.SigningCredentials == null)
                throw new ArgumentNullException(nameof(JwtIssuerOptions.SigningCredentials));
            //if (options.JtiGenerator == null)
            //    throw new ArgumentNullException(nameof(JwtIssuerOptions.JtiGenerator));
        }

        public int GetJtiRndNumber(string jti)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(jti);
            int iSeed = BitConverter.ToInt32(buffer, 0);
            var number = new Random(iSeed).Next(100000000, 1000000000);
            return number;
        }

        public async Task<string> GenerateToken(int userId, string authType)
        {
            //生成随机数
            var jti = Guid.NewGuid().ToString();

            var claims = new[]{
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim(ClaimTypes.Role, authType),
                        new Claim(JwtRegisteredClaimNames.Jti, jti),
                        new Claim(JwtRegisteredClaimNames.Iat, MathUtils.ToUnixEpochDate(_jwtOptions.IssueAt).ToString(), ClaimValueTypes.Integer64),
                    };

            //生成 jwt 安全token, 并编码
            var jwt = new JwtSecurityToken(
                    issuer: _jwtOptions.Issuer,
                    audience: _jwtOptions.Audience,
                    claims: claims,
                    notBefore: _jwtOptions.NotBefore,
                    expires: _jwtOptions.Expiration,
                    signingCredentials: _jwtOptions.SigningCredentials
                );
            string token = new JwtSecurityTokenHandler().WriteToken(jwt);
            return await Task.FromResult(token);
        }

        public JwtIssuerOptions GetJwtOptions()
        {
            return _jwtOptions;
        }
    }
}
