using SpookVooper.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SpookVooper.Web.Models.ForumViewModels
{
    public class PostViewModel
    {
        public ulong PostID { get; set; }

        // The author of the post
        public string Author { get; set; }

        // Category this blog was posted in
        [Required]
        public string Category { get; set; }

        // Title of this post
        [Display(Name = "Post Title")]
        [Required]
        [MaxLength(32, ErrorMessage = "Name should be under 32 characters.")]
        public string Title { get; set; }

        // Content of this post
        [Display(Name = "Post Content")]
        [MaxLength(3000, ErrorMessage = "Post should be under 3000 characters.")]
        public string Content { get; set; }

        [Display(Name = "Image Link")]
        [DataType(DataType.ImageUrl, ErrorMessage = "Please enter a valid image URL")]
        public string ImageLink { get; set; }

        [Display(Name = "Tags")]
        [Required]
        [MaxLength(50, ErrorMessage = "Tags should be under 50 characters.")]
        [RegularExpression("^[a-zA-Z0-9, ]*$", ErrorMessage = "Please use only letters, numbers, and commas.")]
        public string Tags { get; set; }

        // Likes on this post
        public int Likes { get; set; }

        // Is this a picture post?
        public bool Picture { get; set; }

        // Time this blog was posted
        public DateTime TimePosted { get; set; }
    }
}
