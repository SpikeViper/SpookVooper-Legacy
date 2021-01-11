﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SpookVooper.Web.Government
{
    public class Ministry
    {
        [Key]
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
