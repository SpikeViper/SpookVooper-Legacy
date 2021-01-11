using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using SpookVooper.Web.Entities.Groups;
using SpookVooper.Web.Entities;
using SpookVooper.Web.Economy;
using SpookVooper.Web.Economy.Stocks;
using SpookVooper.Web.Government;
using SpookVooper.Web.Government.Voting;
using SpookVooper.Web.Oauth2;
using SpookVooper.Data;
using System;
using System.Collections.Generic;
using System.Text;
using SpookVooper.Web.Economy;
using SpookVooper.Web;
using SpookVooper.Web.Forums;
using SpookVooper.Web.News;

namespace SpookVooper.Web.DB
{
    public class VooperContext : IdentityDbContext<User>
    {
        // Forum stuff
        public DbSet<ForumPost> ForumPosts { get; set; }
        public DbSet<ForumCategory> ForumCategories { get; set; }
        public DbSet<ForumLike> ForumLikes { get; set; }
        public DbSet<ForumComment> ForumComments { get; set; }
        public DbSet<ForumCommentLike> ForumCommentLikes { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        public List<SelectListItem> GetForumCategoryListForDropdown()
        {
            List<SelectListItem> items = new List<SelectListItem>();

            foreach (ForumCategory cat in ForumCategories)
            {
                items.Add(new SelectListItem()
                {
                    Text = cat.CategoryID,
                    Value = cat.CategoryID
                });
            }

            return items;
        }

        // Stock stuff
        public DbSet<StockDefinition> StockDefinitions { get; set; }
        public DbSet<StockObject> StockObjects { get; set; }
        public DbSet<StockOffer> StockOffers { get; set; }

        // Group stuff
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupRole> GroupRoles { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<GroupRoleMember> GroupRoleMembers { get; set; }
        public DbSet<GroupInvite> GroupInvites { get; set; }
        public DbSet<GroupBan> GroupBans { get; set; }
        public DbSet<NewsPost> NewsPosts { get; set; }
        public DbSet<PressPass> PressPasses { get; set; }

        // Government stuff
        public DbSet<District> Districts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<ValueHistory> ValueHistory { get; set; }
        public DbSet<GovControls> GovControls { get; set; }
        public DbSet<Election> Elections { get; set; }
        public DbSet<ElectionVote> ElectionVotes { get; set; }
        public DbSet<CandidatePass> CandidatePasses { get; set; }
        public DbSet<Ministry> Ministries { get; set; }
        public DbSet<Minister> Ministers { get; set; }

        // User stuff
        public DbSet<User> Users { get; set; }
        public DbSet<AuthToken> AuthTokens { get; set; }

        // Oauth stuff
        public DbSet<OauthApp> OauthApps { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseMySql(Secrets.DBstring, ServerVersion.FromString("8.0.20-mysql"), options => options.EnableRetryOnFailure().CharSet(CharSet.Utf8Mb4));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public static DbContextOptions DBOptions;

        public VooperContext(DbContextOptions options)
        {

        }
    }
}
