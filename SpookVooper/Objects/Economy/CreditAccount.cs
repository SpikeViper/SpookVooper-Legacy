using SpookVooper.Web.DB;
using SpookVooper.Web.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpookVooper.Web.Economy
{
    public class CreditAccount
    {
        /// <summary>
        /// The ID of the credit account. Begins with 'ca-'
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The ID of the owner Entity
        /// </summary>
        public string Owner_Id { get; set; }

        /// <summary>
        /// The balance of the account
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// Returns the Entity this account belongs to (async)
        /// </summary>
        /// <returns></returns>
        public async Task<Entity> GetOwnerAsync()
        {
            return await Entity.FindAsync(Owner_Id);
        }

        /// <summary>
        /// Returns the Entity this account belongs to
        /// </summary>
        /// <returns></returns>
        public Entity GetOwner()
        {
            return GetOwnerAsync().Result;
        }
    }
}
