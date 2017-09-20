using MySql.Data.MySqlClient;
using Apollo.Models.Apollo;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using Apollo.Models;
using SpotifyAPI.Web.Models;
using SpotifyAPI.Web;

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

        private static List<string> GetSortedRelatedArtistsByCount(string userID, SpotifyWebAPI spotify) {
            
            // Initialize the counted map that will be used for related artist proportions.
            SortedDictionary<string, int> artistCount = new SortedDictionary<string, int>();

            // Open a connection to the database.
            using (ApolloDBHandler dbHandler = new ApolloDBHandler()) {
                
                // Get all currently liked albums, and foreach album...
                foreach (Album likedAlbum in dbHandler.GetAlbumsFromBridge(userID, ApolloDBHandler.BridgingTables.LIKED_ALBUMS)) {       

                    // Get related artists to the album artist, and foreach related artist...
                    foreach (FullArtist artist in spotify.GetRelatedArtists(likedAlbum.Artist).Artists) {
                        
                        // If the artist is already in the counted map...
                        if (artistCount.ContainsKey(artist.Id)) {
                            // Add one to the artist's count.
                            artistCount[artist.Id]++;
                        } else {
                            // Otherwise, add the artist to the counted map. //TODO: Change 0 to 1
                            artistCount.Add(artist.Id, 0);
                        }
                    }
                }
            }

            return artistCount.Keys.ToList();
        }

        private void GetAlbumRecommendations(string userID, List<Album> recommendedAlbums) {
            
            // Get spotify authorization token.
            ClientCredentialsAuth auth = new ClientCredentialsAuth() {
                ClientId = SpotifyAPIModel.CLIENT_ID,
                ClientSecret = SpotifyAPIModel.CLIENT_SECRET
            };
            Token token = auth.DoAuth();

            // Establish spotify connection.
            SpotifyWebAPI spotify = new SpotifyWebAPI() {
                TokenType = token.TokenType,
                AccessToken = token.AccessToken,
                UseAuth = true,
            };

            // Get all albums this user has listened to.
            List<Album> listenedAlbums;
            using (ApolloDBHandler dbHandler = new ApolloDBHandler()) {
                listenedAlbums = dbHandler.GetAllListenedAlbums(userID);
            }

            // Get a sorted list of recommend artists.
            List<string> recommendedArtists = GetSortedRelatedArtistsByCount(userID, spotify);

            // Initialize a list of albums.
            Paging<SimpleAlbum> albumsPaging;
            List<FullAlbum> albums = new List<FullAlbum>();

            // Foreach artist starting from the most recommended until there are 6 recommended albums...
            for (int i = 0; recommendedAlbums.Count < 6 && i < recommendedArtists.Count; i++) {
                albums.Clear();
                // Get all of the albums from this artist.
                albumsPaging = spotify.GetArtistsAlbums(recommendedArtists[i], AlbumType.All);
                albumsPaging.Items.ForEach(album => albums.Add(spotify.GetAlbum(album.Id)));
                while (albumsPaging.HasNextPage()) {
                    albumsPaging = spotify.GetNextPage(albumsPaging);
                    albumsPaging.Items.ForEach(album => albums.Add(spotify.GetAlbum(album.Id)));
                }

                // See if any albums can be recommended.
                Album recommenedAlbum = RecommendAnAlbum(albums, listenedAlbums);
                
                // If an album is recommened...
                if(recommenedAlbum != null) {
                    // Open database connection.
                    using (ApolloDBHandler dbHandler = new ApolloDBHandler()) {
                        try {
                            // Try to find the album by the uri in the database.
                            recommenedAlbum = dbHandler.GetAlbum(recommenedAlbum.Uri);
                        } catch (Exception) {
                            // Add the album to the database.
                            dbHandler.InsertAlbum(recommenedAlbum);
                        }
                        // Add the album to recommend.
                        dbHandler.BridgeUserAndAlbum_AlbumURI(userID, recommenedAlbum.Uri, ApolloDBHandler.BridgingTables.RECOMMEND);
                        recommendedAlbums.Add(recommenedAlbum);
                    }
                }
            }
        }

        private Album RecommendAnAlbum(List<FullAlbum> albums, List<Album> listenedAlbums) {
            // Initialize for the loop.
            bool listened;

            // Foreach of the artist's albums or until one has been recommended...
            foreach (FullAlbum album in albums) {
                // Set for this iteration.
                listened = false;

                // Check if the user has listened to this album.
                for (int i = 0; !listened && i < listenedAlbums.Count; i++) {
                    if (listenedAlbums[i].Uri.Equals(album.Uri)) {
                        listened = true;
                    }
                }

                // If the user has not listened to the album...
                if (!listened) {
                    // Return the album.
                    return new Album(album.Name, album.Artists[0].Id, album.Uri, album.Images[0].Url);
                }
            }
            return null;
        }
        #endregion

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

        #region Account
        public ActionResult Account() {
            if (Session[LOGGED_IN_USERNAME_SESSION] == null) {
                return Redirect("Login");
            } else {
                string email;
                using (ApolloDBHandler dbHandler = new ApolloDBHandler()) {
                    email = dbHandler.GetEmail(Session[LOGGED_IN_USERID_SESSION] as string);
                }

                return View(new User() { Username = Session[LOGGED_IN_USERNAME_SESSION] as string, Email = email });
            }
        }
        #endregion
    }
}