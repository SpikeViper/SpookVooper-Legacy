using SpookVooper.Web.DB;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SpookVooper.Web.Entities
{
    public interface ITradeable
    {
        /// <summary>
        /// The ID of the owner of this object. Can be any Entity.
        /// </summary>
        public string Owner_Id { get; set; }

        /// <summary>
        /// Returns the value of this object
        /// </summary>
        public decimal GetValue();

        /// <summary>
        /// Returns the value of this object (async)
        /// </summary>
        public Task<decimal> GetValueAsync();

        /// <summary>
        /// Returns true if the entity owns this object
        /// </summary>
        public bool IsOwner(Entity entity);

        /// <summary>
        /// Returns true if the entity owns this object (async)
        /// </summary>
        public Task<bool> IsOwnerAsync(Entity entity);

        /// <summary>
        /// Returns the owner of the object
        /// </summary>
        public Task<Entity> GetOwner();

        /// <summary>
        /// Sets the owner of this object
        /// </summary>
        public Task SetOwnerAsync(Entity newOwner);
    }
}
