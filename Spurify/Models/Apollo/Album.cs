using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spurify.Models.Apollo {
    public class Album {

        public string Name { get; set; }

        public string Uri { get; set; }

        public string ImageLink { get; set; }

        public Album(string name, string uri, string imageLink) {
            Name = name;
            Uri = uri;
            ImageLink = imageLink;
        }

    }

    public class RecommendedAlbums : List<Album> { }
}