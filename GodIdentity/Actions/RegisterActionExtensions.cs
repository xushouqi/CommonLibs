using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GodIdentity.Actions;

namespace GodIdentity
{
    public static class RegisterActionExtensions
    {
        public static IServiceCollection AddRegisterActions(this IServiceCollection services, IHostingEnvironment env, IConfigurationRoot config)
        {
            services.AddTransient<Action1001>();
            services.AddTransient<Action1002>();
            services.AddTransient<Action1003>();
            services.AddTransient<Action1005>();
            services.AddTransient<Action1006>();
            services.AddTransient<Action1007>();
            services.AddTransient<Action1008>();
            services.AddTransient<Action1009>();
            services.AddTransient<Action1010>();
            services.AddTransient<Action1011>();
            services.AddTransient<Action1012>();
            services.AddTransient<Action1013>();
            services.AddTransient<Action1014>();
            services.AddTransient<Action1015>();
            services.AddTransient<Action1016>();
            services.AddTransient<Action1017>();
            services.AddTransient<Action1018>();

            return services;
        }
    }
}
