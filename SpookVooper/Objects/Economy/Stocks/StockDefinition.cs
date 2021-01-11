using SpookVooper.Web.DB;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SpookVooper.Web.Economy.Stocks
{
    public class StockDefinition
    {
        // The ticker, or identifier, for this stock
        [Key]
        public string Ticker { get; set; }

        // The group that issued this stock
        public string Group_Id { get; set; }

        // Current value estimate
        public decimal Current_Value { get; set; }

        public decimal GetYesterdayValue(VooperContext context)
        {
            decimal value = 0;
            var hist = context.ValueHistory.AsQueryable()
                                           .Where(h => h.Account_Id == Ticker && h.Type == "DAY")
                                           .OrderByDescending(h => h.Time)
                                           .FirstOrDefault();

            if (hist != null) value = hist.Value;

            return value;
        }
    }
}
