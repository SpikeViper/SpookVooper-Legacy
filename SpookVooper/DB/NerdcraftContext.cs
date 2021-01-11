using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using SpookVooper.Web;
using SpookVooper.Web.Nerdcraft;
using System;

namespace SpookVooper.Web.DB
{
    public class NerdcraftContext : DbContext
    {

        public DbSet<Player> Players { get; set; }
        public DbSet<Plot> Plots { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseMySql(Secrets.NCDBString, ServerVersion.FromString("8.0.20-mysql"), options => options.EnableRetryOnFailure().CharSet(CharSet.Utf8Mb4));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public NerdcraftContext(DbContextOptions options)
        {

        }
    }
}
