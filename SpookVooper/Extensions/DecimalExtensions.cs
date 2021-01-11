using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpookVooper.Web.Extensions
{
    public static class DecimalExtensions
    {
        public static decimal Round(this decimal dec)
        {
            return Math.Round(dec, 2);
        }
    }
}
