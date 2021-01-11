using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.SignalR;
using SpookVooper.Web;
using SpookVooper.Web.DB;
using SpookVooper.Web.Entities;
using SpookVooper.Web.Government;
using SpookVooper.Web.Economy;
using SpookVooper.Web.Entities.Groups;
using SpookVooper.Web.Hubs;

namespace SpookVooper.Web.Managers
{
    public enum ApplicableTax
    {
        None = 0, Corporate = 1, Payroll = 2, CapitalGains = 3, Sales = 4
    }

    public class TransactionRequest
    {
        [JsonProperty("FromAccount")]
        public string FromAccount;
        [JsonProperty("ToAccount")]
        public string ToAccount;
        [JsonProperty("Amount")]
        public decimal Amount;
        [JsonProperty("Detail")]
        public string Detail;
        [JsonProperty("Force")]
        public bool Force;
        [JsonProperty("IsCompleted")]
        private bool IsCompleted;
        [JsonProperty("Tax")]
        public ApplicableTax Tax;
        [JsonProperty("Result")]
        private TaskResult Result;

        public TransactionRequest(string from, string to, decimal amount, string detail, ApplicableTax tax, bool force = false)
        {
            this.FromAccount = from;
            this.ToAccount = to;
            this.Amount = amount;
            this.Detail = detail;
            this.Force = force;
            this.Tax = tax;
        }

        public void SetResult(TaskResult result)
        {
            IsCompleted = true;
            this.Result = result;
        }

        public async Task<TaskResult> Execute()
        {
            EconomyManager.RequestTransaction(this);

            while (!IsCompleted) await Task.Delay(1);

            return Result;
        }
    }

    public static class EconomyManager
    {
        // Queue for steady and threadsafe transactions
        public static ConcurrentQueue<TransactionRequest> transactionQueue = new ConcurrentQueue<TransactionRequest>();

        public static string VooperiaID = "g-a79c4e06-ca17-4212-8e75-3964e8fe7015";

        public static void RequestTransaction(TransactionRequest request)
        {
            transactionQueue.Enqueue(request);
        }

        public static async Task RunQueue(VooperContext context)
        {
            if (transactionQueue.IsEmpty) return;

            TransactionRequest request;
            bool dequeued = transactionQueue.TryDequeue(out request);

            if (!dequeued) return;

            TaskResult result = await DoTransaction(request, context);

            request.SetResult(result);

            string success = "SUCC";
            if (!result.Succeeded) success = "FAIL";

            Console.WriteLine($"[{success}] Processed {request.Detail} for {request.Amount}.");

            // Notify SignalR
            string json = JsonConvert.SerializeObject(request);

            await TransactionHub.Current.Clients.All.SendAsync("NotifyTransaction", json);
        }

        private static async Task<TaskResult> DoTransaction(TransactionRequest request, VooperContext context)
        {
            if (!request.Force && request.Amount < 0)
            {
                return new TaskResult(false, "Transaction must be positive.");
            }

            if (request.Amount == 0)
            {
                return new TaskResult(false, "Transaction must have a value.");
            }

            if (request.FromAccount == request.ToAccount)
            {
                return new TaskResult(false, $"An entity cannot send credits to itself.");
            }

            Entity fromUser = await Entity.FindAsync(request.FromAccount);
            Entity toUser = await Entity.FindAsync(request.ToAccount);

            if (fromUser == null) { return new TaskResult(false, $"Failed to find sender {request.FromAccount}."); }
            if (toUser == null) { return new TaskResult(false, $"Failed to find reciever {request.ToAccount}."); }

            if (!request.Force && fromUser.Credits < request.Amount)
            {
                return new TaskResult(false, $"{fromUser.Name} cannot afford to send ¢{request.Amount}");
            }

            GovControls govControls = await GovControls.GetCurrentAsync(context);

            Transaction trans = new Transaction()
            {
                ID = Guid.NewGuid().ToString(),
                Credits = request.Amount,
                FromUser = request.FromAccount,
                ToUser = request.ToAccount,
                Details = request.Detail,
                Time = DateTime.Now
            };

            await context.Transactions.AddAsync(trans);

            fromUser.Credits -= request.Amount;
            toUser.Credits += request.Amount;

            context.Update(fromUser);
            context.Update(toUser);

            await context.SaveChangesAsync();

            if (toUser.Id != VooperiaID)
            {
                if (request.Tax == ApplicableTax.None && toUser is Group && ((Group)toUser).Group_Category == Group.GroupTypes.Company)
                {
                    request.Tax = ApplicableTax.Corporate;
                }

                if (request.Tax == ApplicableTax.Payroll)
                {
                    decimal ntax = request.Amount * (govControls.PayrollTaxRate / 100.0m);

                    govControls.UBIAccount += ntax * (govControls.UBIBudgetPercent / 100.0m);

                    govControls.PayrollTaxRevenue += ntax;

                    RequestTransaction(new TransactionRequest(toUser.Id, VooperiaID, ntax, "Payroll Tax", ApplicableTax.None, true));
                }
                else if (request.Tax == ApplicableTax.Sales)
                {
                    decimal ntax = request.Amount * (govControls.SalesTaxRate / 100.0m);

                    govControls.UBIAccount += ntax * (govControls.UBIBudgetPercent / 100.0m);

                    govControls.SalesTaxRevenue += ntax;

                    RequestTransaction(new TransactionRequest(fromUser.Id, VooperiaID, ntax, "Sales Tax", ApplicableTax.None, true));
                }
                else if (request.Tax == ApplicableTax.Corporate)
                {
                    decimal ntax = request.Amount * (govControls.CorporateTaxRate / 100.0m);

                    govControls.UBIAccount += ntax * (govControls.UBIBudgetPercent / 100.0m);

                    govControls.CorporateTaxRevenue += ntax;

                    RequestTransaction(new TransactionRequest(toUser.Id, VooperiaID, ntax, "Corporate Tax", ApplicableTax.None, true));
                }
                else if (request.Tax == ApplicableTax.CapitalGains)
                {
                    decimal ntax = request.Amount * (govControls.CapitalGainsTaxRate / 100.0m);

                    govControls.UBIAccount += ntax * (govControls.UBIBudgetPercent / 100.0m);

                    govControls.CapitalGainsTaxRevenue += ntax;

                    RequestTransaction(new TransactionRequest(fromUser.Id, VooperiaID, ntax, "Capital Gains Tax", ApplicableTax.None, true));
                }


            }
            else
            {
                if (!request.Detail.Contains("Tax") && !request.Detail.Contains("Stock purchase"))
                {
                    govControls.SalesRevenue += request.Amount;
                    govControls.UBIAccount += (request.Amount * (govControls.UBIBudgetPercent / 100.0m));
                }
            }

            context.GovControls.Update(govControls);
            await context.SaveChangesAsync();

            return new TaskResult(true, $"Successfully sent ¢{request.Amount} to {toUser.Name}.");
        }
    }

}
