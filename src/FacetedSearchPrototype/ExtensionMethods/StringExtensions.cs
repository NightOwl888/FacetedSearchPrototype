using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;

namespace FacetedSearchPrototype.ExtensionMethods
{
    public static class StringExtensions
    {
        public static string CleanStringForHtmlId(this string str)
        {
            return Regex.Replace(str, @"[\s-]", "_");
        }
    }
}