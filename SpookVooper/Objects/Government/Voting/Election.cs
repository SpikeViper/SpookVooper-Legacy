using Microsoft.AspNetCore.Mvc.Rendering;
using SpookVooper.Web.DB;
using SpookVooper.Web.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpookVooper.Web.Government.Voting
{
    public class Election
    {
        [Display(Name = "Election ID")]
        public string Id { get; set; }

        // District the election is for
        [Display(Name = "District")]
        public string District { get; set; }

        // Time the election began
        [Display(Name = "Start Date")]
        public DateTime Start_Date { get; set; }

        // Time the election ended
        [Display(Name = "Start Date")]
        public DateTime End_Date { get; set; }

        // The resulting winner of the election
        [Display(Name = "Winner ID")]
        public string Winner_Id { get; set; }

        // False if the election has been ended
        [Display(Name = "Active")]
        public bool Active { get; set; }

        // The kind of election this is
        [Display(Name = "Type")]
        public string Type { get; set; }


        public static List<SelectListItem> GetElectionTypesListForDropdown()
        {
            List<SelectListItem> items = new List<SelectListItem>();

            items.Add(new SelectListItem() { Text = "Senate", Value = "Senate" });

            return items;
        }

        public async Task<int> GetVoteCount(string candidate, bool withEmperor = false)
        {
            int votes = 0;

            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                foreach (var vote in context.ElectionVotes.AsQueryable().Where(x => x.Election_Id == Id && candidate == x.Choice_Id && !x.Invalid))
                {
                    User voter = await context.Users.FindAsync(vote.User_Id);

                    if (voter != null && (withEmperor || voter.district == District))
                    {
                        votes++;
                    }
                }
            }

            return votes;
        }

        public class ResultData
        {
            public User Candidate { get; set; }
            public int Votes { get; set; }

            public ResultData(User cand, int votes)
            {
                this.Candidate = cand;
                this.Votes = votes;
            }
        }

        public async Task<List<ResultData>> GetResults()
        {
            List<ResultData> results = new List<ResultData>();

            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                Dictionary<string, int> votecounts = new Dictionary<string, int>();

                foreach (ElectionVote vote in context.ElectionVotes.AsQueryable().Where(x => x.Election_Id == Id && !x.Invalid))
                {
                    if (!votecounts.ContainsKey(vote.Choice_Id))
                    {
                        votecounts.Add(vote.Choice_Id, 1);
                    }
                    else
                    {
                        votecounts[vote.Choice_Id] += 1;
                    }
                }

                foreach (KeyValuePair<string, int> result in votecounts)
                {
                    User user = await context.Users.FindAsync(result.Key);

                    if (user != null)
                    {
                        results.Add(new ResultData(user, result.Value));
                    }
                }
            }

            results.OrderByDescending(x => x.Votes);

            return results;
        }
    }
}
