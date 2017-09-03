using MySql.Data.MySqlClient;
using Spurify.Models.Apollo;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace Spurify.Controllers {
    public class ApolloController : Controller {
        public const string LOGGEDIN_SESSION = "LoggedIn";
        public const string LOGIN_ERROR_SESSION = "LoginError";

        public ActionResult Index() {
            return View();
        }

        public ActionResult Discover() {
            if(Session[LOGGEDIN_SESSION] == null) {
                Redirect("Login");
            }

            return View(GetRecommenedAlbums());
        }

        private RecommendedAlbums GetRecommenedAlbums() {
            // Get userID
            string userID = GetUserID(Session[LOGGEDIN_SESSION].ToString());

            //Retrieve albums currently in recommend table
            // Setup SQL.
            string sql = "SELECT albumName, albumURI, albumImageLink FROM recommend JOIN albums USING(album_id) WHERE user_id = @userID";
            MySqlCommand query = new MySqlCommand(sql);
            query.Parameters.AddWithValue("@userID", userID);

            // Query Database.
            MySqlDataReader albumsResult = QueryDatabase(query);

            // Read Albums into RecommendedAlbums
            RecommendedAlbums recommendedAlbums = new RecommendedAlbums();
            while (albumsResult.Read()) {
                recommendedAlbums.Add(new Album(albumsResult.GetString(0), albumsResult.GetString(1), albumsResult.GetString(2)));
            }

            // If the number of albums is less than 6...
            if(recommendedAlbums.Count < 6) {
                // Get more recommendations.
                GetAlbumRecommendations(userID, recommendedAlbums);
            }

            // Return RecommendedAlbums
            return recommendedAlbums;
        }

        private void GetAlbumRecommendations(string userID, RecommendedAlbums recommendedAlbums) {    

            // Get all albumArtists in currently liked albums.
            // Setup SQL.
            string sql = "SELECT albumArtist FROM liked_albums JOIN albums USING(album_id) WHERE user_id = @userID";
            MySqlCommand query = new MySqlCommand(sql);
            query.Parameters.AddWithValue("@userID", userID);

            // Query Database.
            MySqlDataReader albumsResult = QueryDatabase(query);

            // Get spotify authorization token.
            string authToken = SpotifyAPI.GetAccessTokenClientCredentialFlow();

            SortedDictionary<string, int> artistCount = new SortedDictionary<string, int>();
           
            // Foreach artist...
            while (albumsResult.Read()) {
                // Add related artists to the counted map.
                foreach (string artist in SpotifyAPI.GetRelatedArtistIds(albumsResult.GetString(0), authToken)) {
                    if (artistCount.ContainsKey(artist)) {
                        artistCount[artist]++;
                    } else {
                        artistCount.Add(artist, 0);
                    }
                } 
            }

            // Foreach artist starting from the most recommended until there are 6 recommended albums.
            foreach (var artistID in artistCount) {

                // Foreach album...
                foreach (var albumUri in SpotifyAPI.GetArtistAlbumURIs(artistID.Key)) {

                    // Check if album exists in database.
                    // Setup SQL.
                    sql = "SELECT album_id FROM albums WHERE albumURI = @albumUri";
                    query = new MySqlCommand(sql);
                    query.Parameters.AddWithValue("@albumUri", albumUri);

                    // If album does not exist...
                    if (ScalarQueryDatabase(query) == null) {
                        // Add album to database.
                        
                        // Add to recommended and exit to next artist.
                        // Setup SQL.
                        sql = "INSERT INTO recommend (user_id, album_id) VALUES (@userID, @album_id)";

                        //recommendedAlbums.Add();
                    } else {
                        // Album exists, check if it is in this user's liked or passed albums.

                        // If the album has not been listened to...
                        if(true) {
                            // Add to recommended and exit to next artist.

                        }
                    }
                }
            }
        }

        #region Login

        public ActionResult Login() {
            return View();
        }

        public void LoginDBHandler(string loginUsername, string loginPassword) {

            #region Validate forms
            if (loginUsername.Length <= 0 || loginPassword.Length <= 0) {
                Session[LOGIN_ERROR_SESSION] = "All forms must be filled.";
                return;
            }
            if (loginUsername.Length > 20) {
                Session[LOGIN_ERROR_SESSION] = "Username cannot be greater than 20 characters.";
                return;
            }
            #endregion

            #region Login user
            // Setup SQL.
            string sql = "SELECT username FROM user WHERE username = @username AND password = @password";
            MySqlCommand query = new MySqlCommand(sql);
            query.Parameters.AddWithValue("@username", loginUsername);
            query.Parameters.AddWithValue("@password", loginPassword);

            // Query Database.
            object result = ScalarQueryDatabase(query);

            // Check result.
            if (result != null) {
                Session[LOGGEDIN_SESSION] = result.ToString();
            } else {
                Session[LOGIN_ERROR_SESSION] = "Invalid username or password.";
            }
            #endregion
        }

        public void RegisterDBHandler(string regUsername, string regPassword, string regEmail) {

        #region Validate forms
                    if (regUsername.Length <= 0 || regPassword.Length <= 0 || regEmail.Length <= 0) {
                        Session[LOGIN_ERROR_SESSION] = "All forms must be filled.";
                        return;
                    }
                    if (regUsername.Length > 20) {
                        Session[LOGIN_ERROR_SESSION] = "Username cannot be greater than 20 characters.";
                        return;
                    }
                    if (regEmail.Length > 40) {
                        Session[LOGIN_ERROR_SESSION] = "Email cannot be greater than 40 characters.";
                        return;
                    }
        #endregion

        #region Check if username exists
                    // Setup SQL.
                    string sql = "SELECT * FROM user WHERE username = @username";
                    MySqlCommand query = new MySqlCommand(sql);
                    query.Parameters.AddWithValue("@username", regUsername);

                    // Query Database.
                    object result = ScalarQueryDatabase(query);

                    // Check result.
                    if (result != null) {
                        Session[LOGIN_ERROR_SESSION] = "Username already exists.";
                        return;
                    }
        #endregion

        #region Create new user
                    // Setup SQL.
                    sql = "INSERT INTO user (username, password, email) VALUES (@username, @password, @email)";
                    query = new MySqlCommand(sql);
                    query.Parameters.AddWithValue("@username", regUsername);
                    query.Parameters.AddWithValue("@password", regPassword);
                    query.Parameters.AddWithValue("@email", regEmail);

                    // Query Database.
                    if (NonQueryDatabase(query) == -1) {
                        Session[LOGIN_ERROR_SESSION] = "Error with registration.";
                    } else {
                        Session[LOGGEDIN_SESSION] = regUsername;
                    }
        #endregion
        }

        #endregion

        #region Database

        private string GetUserID(string username) {
            // Setup SQL.
            string sql = "SELECT user_id FROM user WHERE username = @username";
            MySqlCommand query = new MySqlCommand(sql);
            query.Parameters.AddWithValue("@username", username);

            // Query Database.
            object result = ScalarQueryDatabase(query);

            // Check result.
            if (result != null) {
                return result.ToString();
            } else {
                throw new Exception("Unable to retrieve user_id");
            }
        }

        private MySqlDataReader QueryDatabase(MySqlCommand query) {
            string connectionString = ConfigurationManager.AppSettings["DatabaseConnectionString"];
            MySqlConnection dbConnection = new MySqlConnection(connectionString);
            dbConnection.Open();

            query.Connection = dbConnection;
            MySqlDataReader result = query.ExecuteReader();
            dbConnection.Close();

            return result;
        }

        private object ScalarQueryDatabase(MySqlCommand query) {
            string connectionString = ConfigurationManager.AppSettings["DatabaseConnectionString"];
            MySqlConnection dbConnection = new MySqlConnection(connectionString);
            dbConnection.Open();

            query.Connection = dbConnection;
            object result = query.ExecuteScalar();
            dbConnection.Close();

            return result;
        }

        private int NonQueryDatabase(MySqlCommand query) {
            string connectionString = ConfigurationManager.AppSettings["DatabaseConnectionString"];
            MySqlConnection dbConnection = new MySqlConnection(connectionString);
            dbConnection.Open();

            query.Connection = dbConnection;
            int result = query.ExecuteNonQuery();
            dbConnection.Close();

            return result;
        }

        #endregion
    }
}