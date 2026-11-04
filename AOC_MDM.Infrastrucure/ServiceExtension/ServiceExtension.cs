//using Microsoft.EntityFrameworkCore;

//using Microsoft.Extensions.DependencyInjection;
//using AOC_MDM.Core.Interfaces;
//using AOC_MDM.Infrastrucure.Repository;
//using UOW = AOC_MDM.Infrastrucure.UnitOfWork;
//using System.Data.Odbc;


//namespace AOC_MDM.Infrastrucure.ServiceExtension
//{
//    public static class ServiceExtension
//    {
//        public static IServiceCollection AddDIServices(this IServiceCollection services, IConfiguration configuration)
//        {
//            services.AddDbContext<AOCMDMDbContext>(options =>
//            {
//                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
//            });
//            services.AddScoped<IUnitOfWork, UOW.UnitOfWork>();
//            services.AddScoped<IETLBatchSrcRepository, ETLBatchSRCRepository>();
//            services.AddScoped<IETLBatchRepository, ETLBatchRepository>();
//            services.AddScoped<IETLBatchSrcStepRepository, ETLBatchSrcStepRepository>();
//            services.AddScoped<IETLBatchStepCfgRepository, ETLBatchStepCfgRepository>();
//            services.AddScoped<IETLJobsRepository, ETLJobsRepository>();
//            services.AddScoped<IETLBatchSrcStepCfgRepository, ETLBatchSrcStepCfgRepository>();
//            services.AddScoped<IETLBatchStepPRGMRepository, ETLBatchStepPRGMRepository>();
//            services.AddScoped<IETLJobLogRepository, ETLJobLogRepository>();
//            services.AddScoped<IETLJobLogDtlRepository, ETLJobLogDtlRepository>();
//            services.AddScoped<IETLAppsLogRepository, ETLAppsLogRepository>();

//            services.AddScoped<IETLBatchSrcStepSortedRepository, ETLBatchSrcStepSortedRepository>();

//            return services;
//        }
//    }
//}
