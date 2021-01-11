using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace SpookVooper.Web.Helpers
{
    public static class StringHelper
    {

        public static List<string> allowedTags = new List<string>()
        {
            "h1", "h2", "h3", "h4", "b", "strong", "br", "i", "hr", "ol", "ul", "li", "code", "a",
            "/h1", "/h2", "/h3", "/h4", "/b", "/strong", "/br", "/i", "/hr", "/ol", "/ul", "/li", "/code", "/a",
            "br/", "hr/"
        };

        public static string FormatHTML(string input, int length = int.MaxValue)
        {
            string encoded = HtmlEncoder.Default.Encode(input);

            // Fix encoded that are allowed
            encoded = encoded.Replace("&#x27;", "'")
                             .Replace("&quot;", "\"")
                             .Replace("&#x2019;", "'")
                             .Replace("&lt;", "<")
                             .Replace("&gt;", ">")
                             .Replace("&#xA;", "<br/>")
                             .Replace("&#xD;", "");

            // More complex tag implementations

            string pass2 = "";
            string hotblock = "";
            bool hot = false;

            foreach (char c in encoded)
            {
                if (!hot)
                {
                    if (c == '<')
                    {
                        // Tag is live
                        hot = true;
                        hotblock = "";
                    }
                    else
                    {
                        pass2 += c;
                    }
                }
                else
                {
                    if (c == '>' || c == ' ')
                    {
                        // Tag closed
                        hot = false;

                        bool allow = false;

                        foreach (string allowed in allowedTags)
                        {
                            if (hotblock == allowed)
                            {
                                if (hotblock.Contains('/'))
                                {
                                    allow = true;
                                    break;
                                }
                                else
                                {
                                    int startcount = Regex.Matches(encoded, "<" + hotblock).Count - Regex.Matches(encoded, hotblock + "/>").Count;
                                    int endcount = Regex.Matches(encoded, "</" + hotblock).Count;

                                    // Console.WriteLine($"Matching | {hotblock} | {startcount} | {endcount}");

                                    if (startcount == endcount)
                                    {
                                        allow = true;
                                        break;
                                    }
                                }
                            }
                        }

                        hotblock += c;

                        if (allow)
                        {
                            pass2 += ('<' + hotblock);
                        }
                    }
                    else
                    {
                        hotblock += c;
                    }
                }
            }

            encoded = pass2;

            // Clear out any unwanted HTML
            //encoded = encoded.Replace('&', ' ');

            // Max size
            if (encoded.Length > length)
            {
                encoded = encoded.Substring(0, length);

                encoded += "...";
            }

            return encoded;
        }
        public static string FormatTitle(string input, int length = int.MaxValue)
        {
            string encoded = HtmlEncoder.Default.Encode(input)
                             .Replace("&#x27;", "'")
                             .Replace("&quot;", "\"")
                             .Replace("&#x2019;", "'")
                             .Replace("&lt;", "<")
                             .Replace("&gt;", ">")
                             .Replace("&#xD;&#xA;", "...");

            string cleaned = "";
            bool tag = false;

            // Clean out all html tags
            foreach (char c in encoded)
            {
                if (!tag)
                {
                    if (c == '<')
                    {
                        tag = true;
                    }
                    else
                    {
                        cleaned += c;
                    }
                }
                else
                {
                    if (c == '>')
                    {
                        tag = false;
                    }
                }
                
            }

            // Max size
            if (cleaned.Length > length)
            {
                cleaned = cleaned.Substring(0, length);

                cleaned += "...";
            }

            return cleaned;
        }
    }
}
