
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CommonLibs;
using CommonServices;

namespace GodModels
{
    public static class RegServiceExtensions
    {
        //注册数据对象仓库
        public static IServiceCollection AddRegServices(this IServiceCollection services, IHostingEnvironment env, IConfigurationRoot config)
        {
            services.AddSingleton<EntityContainer<int, BattleConfig, MainDbContext>>();
            services.AddSingleton<EntityContainer<int, TeamUpgrade, MainDbContext>>();
            services.AddSingleton<EntityContainer<int, Cup, MainDbContext>>();
            services.AddSingleton<EntityContainer<int, CupApply, MainDbContext>>();
            services.AddSingleton<EntityContainer<int, Formation, MainDbContext>>();
            services.AddSingleton<EntityContainer<int, Mail, MainDbContext>>();
            services.AddSingleton<EntityContainer<int, Match, MainDbContext>>();
            services.AddSingleton<EntityContainer<int, MatchEvent, MainDbContext>>();
            services.AddSingleton<EntityContainer<int, Player, MainDbContext>>();
            services.AddSingleton<EntityContainer<int, Reward, MainDbContext>>();
            services.AddSingleton<EntityContainer<int, Role, MainDbContext>>();
            services.AddSingleton<EntityContainer<int, Schedule, MainDbContext>>();
            services.AddSingleton<EntityContainer<int, Team, MainDbContext>>();
            services.AddSingleton<EntityContainer<int, TeamMoneyLog, MainDbContext>>();
            services.AddSingleton<EntityContainer<int, TransferBid, MainDbContext>>();
            services.AddSingleton<EntityContainer<int, TransferListing, MainDbContext>>();
            services.AddSingleton<EntityContainer<int, Account, MainDbContext>>();


			return services;
        }
    }
}
