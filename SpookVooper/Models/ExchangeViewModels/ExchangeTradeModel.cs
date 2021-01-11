using SpookVooper.Web.Economy.Stocks;
using SpookVooper.Web.Entities;

namespace SpookVooper.Web.Models.ExchangeViewModels
{
    public class ExchangeTradeModel
    {
        public StockDefinition Stock { get; set; }
        public Entity Chosen_Account { get; set; }
    }
}
