using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GodModels.ViewModels;
using AutoMapper;
using CommonLibs;

namespace GodModels
{
    public static class AutoMapperExtensions
    {
        public static IServiceCollection AddMapperModels(this IServiceCollection services, IHostingEnvironment env, IConfigurationRoot config)
        {
            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<BattleConfig, BattleConfigData>();
                cfg.CreateMap<TeamUpgrade, TeamUpgradeData>();
                cfg.CreateMap<Cup, CupData>();
                cfg.CreateMap<CupApply, CupApplyData>();
                cfg.CreateMap<Formation, FormationData>();
                cfg.CreateMap<Mail, MailData>();
                cfg.CreateMap<Match, MatchData>();
                cfg.CreateMap<MatchEvent, MatchEventData>();
                cfg.CreateMap<Player, PlayerData>();
                cfg.CreateMap<Reward, RewardData>();
                cfg.CreateMap<Role, RoleData>();
                cfg.CreateMap<Schedule, ScheduleData>();
                cfg.CreateMap<Team, TeamData>();
                cfg.CreateMap<TeamMoneyLog, TeamMoneyLogData>();
                cfg.CreateMap<TransferBid, TransferBidData>();
                cfg.CreateMap<TransferListing, TransferListingData>();
                cfg.CreateMap<Account, AccountData>();

            });
            return services;
        }
    }
}
