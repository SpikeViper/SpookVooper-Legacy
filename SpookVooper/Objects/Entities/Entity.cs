using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using SpookVooper.Web.DB;
using SpookVooper.Web.Entities.Groups;
using SpookVooper.Web.Oauth2;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Query;
using SpookVooper.Api.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SpookVooper.Web.Entities
{
    public interface Entity
    {
        public string Id { get; }
        public string Name { get; }
        public decimal Credits { get; set; }
        public string Image_Url { get; }
        public decimal Credits_Invested { get; set; }
        public string Api_Key { get; set; }

        /// <summary>
        /// Add an easy method to find any entity to VooperContext
        /// </summary>
        public static async Task<Entity> FindAsync(string svid)
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                Entity entity = null;

                if (svid == null || string.IsNullOrWhiteSpace(svid))
                {
                    return null;
                }

                if (svid.StartsWith("u-"))
                {
                    entity = (Entity)(await context.Users.FindAsync(svid));
                }
                else if (svid.StartsWith("g-"))
                {
                    entity = (Entity)(await context.Groups.FindAsync(svid));
                }

                return entity;
            }
        }

        public Task<bool> HasPermissionWithKey(string key, string permission);

        public bool HasPermission(Entity entity, string perm);

        public Task<bool> HasPermissionAsync(Entity entity, string perm);

        public decimal GetYesterdayPortfolioValue()
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {

                decimal value = 0;
                var hist = context.ValueHistory.AsQueryable()
                                               .Where(h => h.Account_Id == Id && h.Type == "DAY")
                                               .OrderByDescending(h => h.Time)
                                               .FirstOrDefault();

                if (hist != null) value = hist.Value;

                return value;
            }
        }

        public async Task<decimal> GetPortfolioValue()
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                var ownedStocks = context.StockObjects.AsQueryable().Where(s => s.Owner_Id == Id);

                if (ownedStocks.Count() > 0)
                {
                    decimal value = 0.0m;

                    foreach (var stock in ownedStocks)
                    {
                        value += stock.Amount * await stock.GetValueAsync();
                    }

                    return value - Credits_Invested;
                }
                else
                {
                    return 0.0m;
                }
            }
        }

        public async Task ModifyBalance(decimal amount)
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                if (this is User)
                {
                    User user = new User { Id = this.Id, Credits = this.Credits + amount };
                    context.Users.Attach(user);
                    context.Entry(user).Property(x => x.Credits).IsModified = true;
                    await context.SaveChangesAsync();

                    //await context.Users.ExecuteSqlInterpolatedAsync($"Egg is an egg");
                    //context.Database.ExecuteSqlRawAsync("")
                }

                else if (this is Group)
                {
                    Group group = new Group { Id = this.Id, Credits = this.Credits + amount };
                    context.Groups.Attach(group);
                    context.Entry(group).Property(x => x.Credits).IsModified = true;
                    await context.SaveChangesAsync();
                }

                this.Credits += amount;
            }
        }

        public EntitySnapshot GetSnapshot(IMapper mapper)
        {
            if (this is User)
            {
                UserSnapshot snapshot = mapper.Map<UserSnapshot>(this);
                return snapshot;
            }
            else if (this is Group)
            {
                GroupSnapshot snapshot = mapper.Map<GroupSnapshot>(this);
                return snapshot;
            }

            return null;
        }
    }
}
