using MySql.Data.MySqlClient;
using Apollo.Models.Apollo;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace Apollo.Controllers {
    public class ApolloController : Controller {

        #region Constants
        public const string LOGGED_IN_USERNAME_SESSION = "LoggedInUsername";
        public const string LOGGED_IN_USERID_SESSION = "LoggedInUserID";
        public const string LOGIN_ERROR_SESSION = "LoginError";
        #endregion

        #region Index
        public ActionResult Index() {
            return View();
        }
        #endregion

        #region Discover
        public ActionResult Discover() {
            if (Session[LOGGED_IN_USERNAME_SESSION] == null) {
                return Redirect("Login");
            }

            return View(GetRecommenedAlbums());
        }
        #endregion

        /// <summary>
        /// Gets the recommened albums.
        /// </summary>
        /// <returns>A <see cref="List{T}"/> of <see cref="Album"/></returns>
        private List<Album> GetRecommenedAlbums() {
            // Get userID from the session.
            string userID = Session[LOGGED_IN_USERID_SESSION].ToString();

            List<Album> recommendedAlbums;

            //Retrieve albums currently in recommend table
            using (ApolloDBHandler dbHandler = new ApolloDBHandler()) {
                recommendedAlbums = dbHandler.GetAlbumsFromBridge(userID, ApolloDBHandler.BridgingTables.RECOMMEND);
            }

            // If the number of albums is less than 6...
            if (recommendedAlbums.Count < 6) {
                // Get more recommendations.
                GetAlbumRecommendations(userID, recommendedAlbums);
            }

            // Return RecommendedAlbums
            return recommendedAlbums;
        }

        private static List<string> GetSortedRelatedArtistsByCount(string userID, string spotifyAuthToken) {
            // Initialize the counted map that will be used for related artist proportions.
            SortedDictionary<string, int> artistCount = new SortedDictionary<string, int>();

            // Open a connection to the database.
            using (ApolloDBHandler dbHandler = new ApolloDBHandler()) {
                // Get all currently liked albums, and foreach album...
                foreach (Album likedAlbum in dbHandler.GetAlbumsFromBridge(userID, ApolloDBHandler.BridgingTables.LIKED_ALBUMS)) {
                    // Get related artists to the album artist, and foreach related artist...
                    foreach (string artist in SpotifyAPI.GetRelatedArtistIds(likedAlbum.Artist, spotifyAuthToken)) {
                        // If the artist is already in the counted map...
                        if (artistCount.ContainsKey(artist)) {
                            // Add one to the artist's count.
                            artistCount[artist]++;
                        } else {
                            // Otherwise, add the artist to the counted map.
                            artistCount.Add(artist, 0);
                        }
                    }
                }
            }

            return artistCount.Keys.ToList();
        }

        private void GetAlbumRecommendations(string userID, List<Album> recommendedAlbums) {
            // Get spotify authorization token.
            string authToken = SpotifyAPI.GetAccessTokenClientCredentialFlow();

            // Get all albums this user has listened to.
            List<Album> listenedAlbums;
            using (ApolloDBHandler dbHandler = new ApolloDBHandler()) {
                listenedAlbums = dbHandler.GetAllListenedAlbums(userID);
            }

            // Get a sorted list of recommend artists.
            List<string> recommendedArtists = GetSortedRelatedArtistsByCount(userID, authToken);

            // Initialize for the next loop.
            bool recommened;
            List<Album> albums;

            // Foreach artist starting from the most recommended until there are 6 recommended albums.
            for (int i = 0; recommendedAlbums.Count < 6 && i < recommendedArtists.Count; i++) {
                // Set for this loop.
                recommened = false;
                albums = SpotifyAPI.GetArtistAlbums(recommendedArtists[i]);

                // Initialize for the next loop.
                bool listened;

                // Foreach of the artist's albums or until one has been recommended...
                for (int j = 0; !recommened && j < albums.Count; j++) {
                    // Set for this loop.
                    listened = false;
                    
                    // Check if the user has listened to this album.
                    for (int k = 0; !listened && k < listenedAlbums.Count; k++) {
                        if (listenedAlbums[k].Uri.Equals(albums[j].Uri)) {
                            listened = true;
                        }
                    }

                    // If the user has not listened to the album...
                    if (!listened) {
                        // Check if album exists in database.
                        Album albumToAdd = albums[j];

                        // Open database connection.
                        using (ApolloDBHandler dbHandler = new ApolloDBHandler()) {
                            try {
                                // Try to find the album by the uri in the database.
                                albumToAdd = dbHandler.GetAlbum(albumToAdd.Uri);
                            } catch (Exception) {
                                // Add the album to the database.
                                dbHandler.InsertAlbum(albumToAdd);
                            }

                            // Add the album to recommend.
                            dbHandler.BridgeUserAndAlbum_AlbumURI(userID, albumToAdd.Uri, ApolloDBHandler.BridgingTables.RECOMMEND);
                            recommendedAlbums.Add(albumToAdd);
                        }
                        recommened = true;
                    }
                }
            }
        }

        #region Login
        public ActionResult Login() {
            return View();
        }

        public ActionResult LoginHandler(string loginUsername, string loginPassword) {

            // Validate forms.
            if (loginUsername.Length <= 0 || loginPassword.Length <= 0) {
                Session[LOGIN_ERROR_SESSION] = "All forms must be filled.";
                return Redirect("Login");
            }
            if (loginUsername.Length > 20) {
                Session[LOGIN_ERROR_SESSION] = "Username cannot be greater than 20 characters.";
                return Redirect("Login");
            }

            // Hash the password.
            // TODO: hash the password.

            // Login
            using (ApolloDBHandler dbHandler = new ApolloDBHandler()) {
                Session[LOGGED_IN_USERID_SESSION] = dbHandler.Login(loginUsername, loginPassword);
                Session[LOGGED_IN_USERNAME_SESSION] = loginUsername;
            }

            return Redirect("Discover");
        }

        public ActionResult RegisterHandler(string regUsername, string regPassword, string regEmail) {

            // Validate forms.
            if (regUsername.Length <= 0 || regPassword.Length <= 0 || regEmail.Length <= 0) {
                Session[LOGIN_ERROR_SESSION] = "All forms must be filled.";
                return Redirect("Login");
            }
            if (regUsername.Length > 20) {
                Session[LOGIN_ERROR_SESSION] = "Username cannot be greater than 20 characters.";
                return Redirect("Login");
            }
            if (regEmail.Length > 40) {
                Session[LOGIN_ERROR_SESSION] = "Email cannot be greater than 40 characters.";
                return Redirect("Login");
            }

            // Check if username already exists.
            using (ApolloDBHandler dbHandler = new ApolloDBHandler()) {
                try {
                    // Try to get a user_id for the provided username
                    dbHandler.GetUserID(regUsername);
                    // If this was successful, return an error.
                    Session[LOGIN_ERROR_SESSION] = "Username already exists.";
                    return Redirect("Discover");
                } catch (Exception) {
                    // User doesn't exists, create the new user.
                    Session[LOGGED_IN_USERID_SESSION] = dbHandler.Register(regUsername, regPassword, regEmail);
                    Session[LOGGED_IN_USERNAME_SESSION] = regUsername;
                    return Redirect("Login");
                }
            }
        }
        #endregion

    }
}