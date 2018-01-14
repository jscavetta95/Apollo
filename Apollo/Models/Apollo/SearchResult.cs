using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Apollo.Models.Apollo
{
    public class SearchResult
    {
        public string Value { get; set; }
        public string Img { get; set; }
        public string Uri { get; set; }

        public SearchResult(string value, string img, string uri)
        {
            Value = value;
            Img = img;
            Uri = uri;
        }
    }
}