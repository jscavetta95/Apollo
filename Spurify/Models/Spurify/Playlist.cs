using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spurify.Models.Spurify {
    public class Playlist {

        #region Properties

        public string Href { get; set; }
        public string Name { get; set; }
        public List<Track> Tracks { get; set; }

        #endregion

        #region Constructor

        public Playlist(string href, string name) {
            Href = href;
            Name = name;
            Tracks = new List<Track>();
        }

        #endregion

        #region Public Methods

        public string GetID() {
            return Href.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)[6];
        }

        #endregion

        #region Override Methods

        public override string ToString() {
            return Name;
        }

        #endregion

    }
}