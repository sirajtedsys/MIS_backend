using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MIS_Backend.Class.DbModel;
using MIS_Backend.Data.DbModel;
using Oracle.ManagedDataAccess.Client;
using System.Xml;

namespace MIS_Backend.Class
{
    public class AppDbContext : DbContext
    {

        protected readonly IConfiguration _configuration;
        protected readonly IServiceProvider _serviceProvider;

        public AppDbContext(IServiceProvider serviceProvider, DbContextOptions<AppDbContext> options, IConfiguration configuration)
             : base(options)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //if (!optionsBuilder.IsConfigured)
            //{
            //    optionsBuilder.UseOracle("User Id=SYSTEM;Password=/:t3d5y5/;Data Source=FORA11G3;"
            //        );
            //}

            //var dbName = httpContextAccessor.HttpContext.Request.Headers["Bookit"].First();

            var httpContextAccessor = _serviceProvider.GetRequiredService<IHttpContextAccessor>();
            string connection = _configuration.GetConnectionString("OracleDbContext");
            optionsBuilder.UseOracle(connection);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            #region UCHEMR

            modelBuilder.Entity<UCHEMR_Emr_Admin_users>().ToTable("EMR_ADMIN_USERS", schema: "UCHEMR");

            modelBuilder.Entity<UCHEMR_Emr_Admin_Users_Branch_Link>().ToTable("EMR_ADMIN_USERS_BRANCH_LINK", schema: "UCHEMR");

            #endregion UCHEMR


            #region UCHMASTER

            modelBuilder.Entity<Tbl_Hrm_Department>().ToTable("HRM_DEPARTMENT", schema: "UCHMASTER");
            modelBuilder.Entity<UCHMASTER_Hrm_Branch>().ToTable("HRM_BRANCH", schema: "UCHMASTER");
            modelBuilder.Entity<UCHMASTER_LoginSettings>().ToTable("LOGIN_SETTINGS", schema: "UCHMASTER");

            #endregion UCHMASTER



            //modelBuilder.Entity<MyEntity>().ToTable("MyTable", "Schema1");
            //modelBuilder.Entity<AnotherEntity>().ToTable("AnotherTable", "Schema2");

            // Other entity configurations can go here
        }

        //SELECT ausr_pwd, ausr_username, ausr_id, ausr_username, ausr_status FROM UCHEMR.EMR_ADMIN_USERS WHERE ausr_status<>'D' AND ausr_username = 'tedsys' AND ausr_pwd = 'ted@123'

        #region UCHMASTER

        public DbSet<UCHMASTER_Hrm_Branch> HRM_BRANCH { get; set; }
        public DbSet<Tbl_Hrm_Department> HRM_DEPARTMENT { get; set; }
        public DbSet<UCHMASTER_LoginSettings> LOGIN_SETTINGS { get; set; }

        #endregion UCHMASTER

        #region UCHEMR

        public DbSet<UCHEMR_Emr_Admin_users> EMR_ADMIN_USERS { get; set; }
        public DbSet<UCHEMR_Emr_Admin_Users_Branch_Link> EMR_ADMIN_USERS_BRANCH_LINK { get; set; }


        #endregion UCHEMR


        #region UCHTRANS

        #endregion UCHTRANS


    }
}
