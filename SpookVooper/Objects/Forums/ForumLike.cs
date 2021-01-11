using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SpookVooper.Web.Forums
{
    public class ForumLike
    {
        [Key]
        public string LikeID { get; set; }
        public string AddedBy { get; set; }
        public string GivenTo { get; set; }
        public ulong Post { get; set; }
    }
}
