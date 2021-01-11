using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;
using SpookVooper.Web.Entities;
using SpookVooper.Web.DB;

namespace SpookVooper.Web.Economy.Stocks
{
    public class StockObject : ITradeable
    {
        // Stock ID is NOT the ticker, it is a UUID for this "stack" of stock
        [Key]
        public string Id { get; set; }

        // The ticker is the unique symbol used to identify the stock
        public string Ticker { get; set; }

        // The ID of the owning entity
        public string Owner_Id { get; set; }

        // The amount of stock in this
        public int Amount { get; set; }

        // The name of the stock item
        public string Name { get { return Ticker + " Stock"; } }

        public decimal GetValue()
        {
            return GetValueAsync().Result;
        }

        public async Task<decimal> GetValueAsync()
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                return (await context.StockDefinitions.FindAsync(Ticker)).Current_Value;
            }
        }
        public bool IsOwner(Entity entity)
        {
            return Owner_Id == entity.Id;
        }

        public async Task<Entity> GetOwner()
        {
            return await Entity.FindAsync(Owner_Id);
        }

        public async Task<bool> IsOwnerAsync(Entity entity)
        {
            return IsOwner(entity);
        }

        public async Task SetOwnerAsync(Entity newOwner)
        {
            // Set locally
            Owner_Id = newOwner.Id;

            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                // Set in DB
                StockObject dummy = new StockObject { Id = this.Id, Owner_Id = Owner_Id };
                context.Attach(dummy);
                context.Entry(dummy).Property(x => x.Owner_Id).IsModified = true;

                await context.SaveChangesAsync();
            }
        }
    }
}
