//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;

//namespace CommonServices
//{
//    public class JwtDbConetxt : DbContext
//    {
//        public DbSet<JwtBlackRecord> Accounts { get; set; }

//        public JwtDbConetxt(DbContextOptions<JwtDbConetxt> options)
//            : base(options)
//        {
//        }

//        protected override void OnModelCreating(ModelBuilder builder)
//        {
//            base.OnModelCreating(builder);
//        }

//        protected override void OnConfiguring(DbContextOptionsBuilder builder)
//        {
//            if (!builder.IsConfigured)
//            {
//                var cbuilder = new ConfigurationBuilder()
//                .AddJsonFile("appsettings.json");
//                var config = cbuilder.Build();

//                var conn = config.GetConnectionString("MySql");
//                builder.UseMySql(conn);
//            }
//            base.OnConfiguring(builder);
//        }
//    }
//}
