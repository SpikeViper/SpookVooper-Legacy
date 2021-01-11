using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Linq;
using SpookVooper.Web.Entities;
using SpookVooper.Web.DB;

namespace SpookVooper.Web.Forums
{
    public class ForumComment
    {
        // ID of this comment
        [Key]
        public ulong CommentID { get; set; }

        // Post this was posted on
        public ulong PostOnID { get; set; }

        // Comment this was commented on
        public ulong? CommentOnID { get; set; }

        // Content of the comment
        public string Content { get; set; }

        // User who posted this comment
        public string UserID { get; set; }

        // Time posted
        public DateTime TimePosted { get; set; }

        public bool Removed { get; set; }

        // Forum comments
        public User GetUser(VooperContext context)
        {
            return context.Users.FirstOrDefault(u => u.Id == UserID);
        }

        // Likes on this comment
        public int GetLikes(VooperContext context)
        {
            return context.ForumCommentLikes.Count(l => l.CommentID == CommentID);
        }

        public float GetWeight(VooperContext context)
        {
            //              One week - Time passed
            float timeDecay = 604800f - (float)DateTime.UtcNow.Subtract(TimePosted).TotalSeconds;

            // Lowest is 1
            timeDecay = Math.Max(1, timeDecay) / 100000f;

            return GetLikes(context) * timeDecay;
        }
    }
}
