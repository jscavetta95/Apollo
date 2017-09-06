using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Apollo.Models {
    public class SpotifyAPIModel {
        
        #region Constants
        public const string CLIENT_ID = "45c5e785d0644c94a41d23b43ebc9890";
        public const string CLIENT_SECRET = "301e67b7fa5a4b3bbd907a59b2942471";

        public const string REDIRECT = "http://localhost:58405/Spurify/Login";
        public const string SCOPE = "playlist-read-private%20playlist-read-collaborative%20playlist-modify-public%20playlist-modify-private";
        public const string SHOW_DIALOG = "true";

        private const string TOKEN_URI = "https://accounts.spotify.com/api/token";
        public const string PROFILE_URI = "https://api.spotify.com/v1/me";
        #endregion
    }
}