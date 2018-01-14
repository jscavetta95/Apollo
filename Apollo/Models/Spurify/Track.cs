using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Apollo.Models.Spurify {
    public class Track {
        
        #region Properties

        public string Name { get; set; }
        public string Artist { get; set; }
        public string Uri { get; set; }
        public int PlayCount { get; set; }

        #endregion

        #region Constructor

        public Track(string name, string artist, string uri) {
            Name = name;
            Artist = artist;
            Uri = uri;
        }

        #endregion

        #region Override Methods

        public override int GetHashCode() {
            return (Name.ToLower() + Artist.ToLower()).GetHashCode();
        }

        public override bool Equals(Object obj) {
            return ((obj is Track) && (Name.Equals(((Track)obj).Name)) && (Artist.Equals(((Track)obj).Artist)));
        }

        public override string ToString() {
            return Name;
        }

        #endregion
    }
}