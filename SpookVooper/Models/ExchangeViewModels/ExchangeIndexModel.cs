using SpookVooper.Web.Economy.Stocks;
using SpookVooper.Web.Entities;
using System.Linq;

namespace SpookVooper.Web.Models.ExchangeViewModels
{
    public class ExchangeIndexModel
    {
        public IQueryable<StockDefinition> Stock_List { get; set; }
        public Entity Chosen_Account { get; set; }
    }
}
