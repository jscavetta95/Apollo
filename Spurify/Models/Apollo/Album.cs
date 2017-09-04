using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spurify.Models.Apollo {
    public class Album {

        public string Name { get; set; }

        public string Artist { get; set; }

        public string Uri { get; set; }

        public string ImageLink { get; set; }

        public Album(string name, string artist, string uri, string imageLink) {
            Name = name;
            Artist = artist;
            Uri = uri;
            ImageLink = imageLink;
        }
    }
}