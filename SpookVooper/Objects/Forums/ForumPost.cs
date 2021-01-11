﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Linq;
using SpookVooper.Web.DB;

namespace SpookVooper.Web.Forums
{
    public class ForumPost
    {
        // The ID of this post
        [Key]
        public ulong PostID { get; set; }

        // The author of the post
        public string Author { get; set; }

        // Category this blog was posted in
        public string Category { get; set; }

        // Title of this post
        public string Title { get; set; }

        // Content of this post
        public string Content { get; set; }

        // Tags of this post
        public string Tags { get; set; }

        public bool Removed { get; set; }

        // Pic post
        public bool Picture { get; set; }

        // Time this blog was posted
        public DateTime TimePosted { get; set; }

        public int GetLikes(VooperContext context)
        {
            return context.ForumLikes.Count(l => l.Post == PostID);
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
