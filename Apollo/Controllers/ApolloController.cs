namespace Apollo.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using Apollo.Models;
    using Apollo.Models.Apollo;
    using SpotifyAPI.Web;
    using SpotifyAPI.Web.Auth;
    using SpotifyAPI.Web.Enums;
    using SpotifyAPI.Web.Models;

    public class ApolloController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Login()
        {
            if (Session[Constants.ErrorSession] != null)
            {
                return View(model: Session[Constants.ErrorSession].ToString());
            }
            else
            {
                return View();
            }
        }

        public ActionResult Account()
        {
            if (Session[Constants.UsernameSession] == null)
            {
                return Redirect("Login");
            }
            else
            {
                string email;
                using (ApolloDBHandler dbHandler = new ApolloDBHandler())
                {
                    email = dbHandler.GetEmail(Session[Constants.UserIDSession] as string);
                }

                return View(new User() { Username = Session[Constants.UsernameSession] as string, Email = email });
            }
        }

        public ActionResult Discover()
        {
            if (Session[Constants.UsernameSession] == null)
            {
                return Redirect("Login");
            }

            try
            {
                return View(GetRecommenedAlbums());
            }
            catch (Exception)
            {
                return Redirect("Albums");
            }
        }

        public ActionResult Albums()
        {
            if (Session[Constants.UsernameSession] == null)
            {
                return Redirect("Login");
            }

            Dictionary<ApolloDBHandler.BridgingTables, List<Album>> listenedAlbumsDictionary = new Dictionary<ApolloDBHandler.BridgingTables, List<Album>>();
            string userID = Session[Constants.UserIDSession].ToString();

            using (ApolloDBHandler dbHandler = new ApolloDBHandler())
            {
                listenedAlbumsDictionary.Add(ApolloDBHandler.BridgingTables.LIKED_ALBUMS, dbHandler.GetAlbumsFromBridge(userID, ApolloDBHandler.BridgingTables.LIKED_ALBUMS));
                listenedAlbumsDictionary.Add(ApolloDBHandler.BridgingTables.PASSED_ALBUMS, dbHandler.GetAlbumsFromBridge(userID, ApolloDBHandler.BridgingTables.PASSED_ALBUMS));
            }

            return View(listenedAlbumsDictionary);
        }

        public ActionResult Logout()
        {
            Session.Abandon();
            return Redirect("Index");
        }

        public ActionResult LoginHandler(string loginUsername, string loginPassword)
        {
            // Validate forms.
            if (loginUsername.Length <= 0 || loginPassword.Length <= 0)
            {
                Session[Constants.ErrorSession] = "All forms must be filled.";
                return Redirect("Login");
            }

            if (loginUsername.Length > 20)
            {
                Session[Constants.ErrorSession] = "Username cannot be greater than 20 characters.";
                return Redirect("Login");
            }

            // Login
            try
            {
                using (ApolloDBHandler dbHandler = new ApolloDBHandler())
                {
                    Session[Constants.UserIDSession] = dbHandler.Login(loginUsername, loginPassword);
                    Session[Constants.UsernameSession] = loginUsername;
                    RefreshSpotifyToken();
                    return Redirect("Discover");
                }
            }
            catch (Exception e)
            {
                Session[Constants.ErrorSession] = e.Message;
                return Redirect("Login");
            }
        }

        public ActionResult RegisterHandler(string regUsername, string regPassword, string confirmPass, string regEmail)
        {
            // Validate forms.
            if (regUsername.Length <= 0 || regPassword.Length <= 0 || regEmail.Length <= 0)
            {
                Session[Constants.ErrorSession] = "All forms must be filled.";
                return Redirect("Login");
            }

            if (regUsername.Length > 20)
            {
                Session[Constants.ErrorSession] = "Username cannot be greater than 20 characters.";
                return Redirect("Login");
            }

            if (regEmail.Length > 40)
            {
                Session[Constants.ErrorSession] = "Email cannot be greater than 40 characters.";
                return Redirect("Login");
            }

            if (!regPassword.Equals(confirmPass))
            {
                Session[Constants.ErrorSession] = "Passwords do not match.";
                return Redirect("Login");
            }

            // Check if username already exists.
            using (ApolloDBHandler dbHandler = new ApolloDBHandler())
            {
                try
                {
                    // Try to get a user_id for the provided username
                    dbHandler.GetUserID(regUsername);

                    // If this was successful, return an error.
                    Session[Constants.ErrorSession] = "Username already exists.";
                    return Redirect("Login");
                }
                catch (Exception)
                {
                    // User doesn't exists, create the new user.

                    // Hash the password.
                    string hashedPass = Crypto.HashPassword(regPassword);

                    Session[Constants.UserIDSession] = dbHandler.Register(regUsername, hashedPass, regEmail);
                    Session[Constants.UsernameSession] = regUsername;

                    RefreshSpotifyToken();
                    return Redirect("Albums");
                }
            }
        }

        public ActionResult ProcessAlbum(string albumURI, bool like)
        {
            using (ApolloDBHandler dbHandler = new ApolloDBHandler())
            {
                if (like)
                {
                    dbHandler.BridgeUserAndAlbum_AlbumURI(Session[Constants.UserIDSession] as string, albumURI, ApolloDBHandler.BridgingTables.LIKED_ALBUMS);
                }
                else
                {
                    dbHandler.BridgeUserAndAlbum_AlbumURI(Session[Constants.UserIDSession] as string, albumURI, ApolloDBHandler.BridgingTables.PASSED_ALBUMS);
                }
            }

            return Redirect("Discover");
        }

        public ActionResult GetAlbum(int index, ApolloDBHandler.BridgingTables table)
        {
            using (ApolloDBHandler dbHandler = new ApolloDBHandler())
            {
                try
                {
                    Album album = dbHandler.GetAlbumsFromBridge(Session[Constants.UserIDSession].ToString(), table, 1, index)[0];
                    return Json(new { imageLink = album.ImageLink, uri = album.Uri });
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public ActionResult SearchForAlbum(string searchInput)
        {
            Token token = Session[Constants.ApolloTokenSession] as Token;
            if (token.IsExpired())
            {
                RefreshSpotifyToken();
            }

            // Establish spotify connection.
            SpotifyWebAPI spotify = new SpotifyWebAPI()
            {
                TokenType = token.TokenType,
                AccessToken = token.AccessToken,
                UseAuth = true,
            };

            List<SearchResult> results = new List<SearchResult>();
            SearchItem searchItem = spotify.SearchItems(searchInput, SearchType.Album);
            if (searchItem.Albums != null && searchItem.Albums.Total > 0)
            {
                searchItem.Albums.Items.ForEach(album => results.Add(new SearchResult(album.Name, album.Images[1].Url, album.Uri)));
            }

            return Json(results);
        }

        public ActionResult AddAlbumSeed(string uri)
        {
            using (ApolloDBHandler dbHandler = new ApolloDBHandler())
            {
                try
                {
                    // Try to find the album by the uri in the database.
                    dbHandler.GetAlbum(uri);
                }
                catch (Exception)
                {
                    // Establish spotify connection.
                    Token token = Session[Constants.ApolloTokenSession] as Token;
                    if (token.IsExpired())
                    {
                        RefreshSpotifyToken();
                    }

                    SpotifyWebAPI spotify = new SpotifyWebAPI()
                    {
                        TokenType = token.TokenType,
                        AccessToken = token.AccessToken,
                        UseAuth = true,
                    };

                    // Get ID from URI
                    string id = uri.Split(':')[2];

                    // Add the album to the database.
                    FullAlbum fullAlbum = spotify.GetAlbum(id);
                    dbHandler.InsertAlbum(new Album(fullAlbum.Name, fullAlbum.Artists[0].Id, fullAlbum.Uri, fullAlbum.Images[0].Url));
                }

                // Add the album to liked.
                dbHandler.BridgeUserAndAlbum_AlbumURI(
                    Session[Constants.UserIDSession].ToString(),
                    uri,
                    ApolloDBHandler.BridgingTables.LIKED_ALBUMS);
            }

            return Redirect("Albums");
        }

        public ActionResult RemoveListenedAlbum(string uri, ApolloDBHandler.BridgingTables table)
        {
            using (ApolloDBHandler dbHandler = new ApolloDBHandler())
            {
                dbHandler.RemoveAlbumFromBridge(Session[Constants.UserIDSession] as string, uri, table);
            }

            return Redirect("Albums");
        }

        public bool ChangePassword(string oldPass, string newPass)
        {
            using (ApolloDBHandler dbHandler = new ApolloDBHandler())
            {
                if (dbHandler.ChangePassword(Session[Constants.UserIDSession] as string, oldPass, newPass))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool ChangeEmail(string newEmail)
        {
            using (ApolloDBHandler dbHandler = new ApolloDBHandler())
            {
                if (dbHandler.ChangeEmail(Session[Constants.UserIDSession] as string, newEmail))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private List<string> GetSortedRelatedArtistsByCount(string userID, SpotifyWebAPI spotify, List<Album> likedAlbums)
        {
            // Initialize the counted map that will be used for related artist proportions.
            SortedDictionary<string, int> artistCount = new SortedDictionary<string, int>();

            // Get all currently liked albums, and foreach album...
            foreach (Album likedAlbum in likedAlbums)
            {
                // Get related artists to the album artist, and foreach related artist...
                foreach (FullArtist artist in spotify.GetRelatedArtists(likedAlbum.Artist).Artists)
                {
                    // If the artist is already in the counted map...
                    if (artistCount.ContainsKey(artist.Id))
                    {
                        // Add one to the artist's count.
                        artistCount[artist.Id]++;
                    }
                    else
                    {
                        // Otherwise, add the artist to the counted map.
                        artistCount.Add(artist.Id, 1);
                    }
                }
            }

            return artistCount.Keys.ToList();
        }

        private List<Album> GetRecommenedAlbums()
        {
            // Get userID from the session.
            string userID = Session[Constants.UserIDSession].ToString();

            List<Album> recommendedAlbums;

            // Retrieve albums currently in recommend table
            using (ApolloDBHandler dbHandler = new ApolloDBHandler())
            {
                recommendedAlbums = dbHandler.GetAlbumsFromBridge(userID, ApolloDBHandler.BridgingTables.RECOMMEND);

                // If the number of albums is less than 6...
                if (recommendedAlbums.Count < 6)
                {
                    // Check if there are any album seeds
                    List<Album> likedAlbums = dbHandler.GetAlbumsFromBridge(userID, ApolloDBHandler.BridgingTables.LIKED_ALBUMS);
                    if (likedAlbums.Count > 0)
                    {
                        // Get more recommendations.
                        GetAlbumRecommendations(userID, recommendedAlbums, likedAlbums, dbHandler);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }

            // Return RecommendedAlbums
            return recommendedAlbums;
        }

        private void GetAlbumRecommendations(string userID, List<Album> recommendedAlbums, List<Album> likeAlbums, ApolloDBHandler dbHandler)
        {
            Token token = Session[Constants.ApolloTokenSession] as Token;
            if (token.IsExpired())
            {
                RefreshSpotifyToken();
            }

            // Establish spotify connection.
            SpotifyWebAPI spotify = new SpotifyWebAPI()
            {
                TokenType = token.TokenType,
                AccessToken = token.AccessToken,
                UseAuth = true,
            };

            // Get all albums this user has listened to.
            List<Album> listenedAlbums;
            listenedAlbums = dbHandler.GetAllListenedAlbums(userID);

            // Get a sorted list of recommend artists.
            List<string> recommendedArtists = GetSortedRelatedArtistsByCount(userID, spotify, likeAlbums);

            // Initialize a list of albums.
            Paging<SimpleAlbum> albumsPaging;

            // Foreach artist starting from the most recommended until there are 6 recommended albums...
            for (int i = 0; recommendedAlbums.Count < 6 && i < recommendedArtists.Count; i++)
            {
                // Get a page of albums from this artist.
                albumsPaging = spotify.GetArtistsAlbums(recommendedArtists[i], AlbumType.Album, market: "US");

                // See if any albums can be recommended.
                Album recommenedAlbum = RecommendAnAlbum(albumsPaging.Items, listenedAlbums, recommendedArtists[i], spotify);

                // If no albums are recommended on first page, try the next.
                while (recommenedAlbum == null && albumsPaging.HasNextPage())
                {
                    albumsPaging = spotify.GetNextPage(albumsPaging);
                    recommenedAlbum = RecommendAnAlbum(albumsPaging.Items, listenedAlbums, recommendedArtists[i], spotify);
                }

                // If an album is recommened...
                if (recommenedAlbum != null)
                {
                    try
                    {
                        // Try to find the album by the uri in the database.
                        recommenedAlbum = dbHandler.GetAlbum(recommenedAlbum.Uri);
                    }
                    catch (Exception)
                    {
                        // Add the album to the database.
                        dbHandler.InsertAlbum(recommenedAlbum);
                    }

                    // Add the album to recommend.
                    dbHandler.BridgeUserAndAlbum_AlbumURI(userID, recommenedAlbum.Uri, ApolloDBHandler.BridgingTables.RECOMMEND);
                    recommendedAlbums.Add(recommenedAlbum);
                }
            }
        }

        private Album RecommendAnAlbum(List<SimpleAlbum> albums, List<Album> listenedAlbums, string artistID, SpotifyWebAPI spotify)
        {
            // Initialize for the loop.
            bool listened;

            // Foreach of the artist's albums or until one has been recommended...
            foreach (SimpleAlbum album in albums)
            {
                // Set for this iteration.
                listened = false;

                // Check if the user has listened to this album.
                for (int i = 0; !listened && i < listenedAlbums.Count; i++)
                {
                    if (listenedAlbums[i].Uri.Equals(album.Uri) || (listenedAlbums[i].Name.Equals(album.Name) && listenedAlbums[i].Artist.Equals(artistID)))
                    {
                        listened = true;
                    }
                }

                // If the user has not listened to the album...
                if (!listened)
                {
                    // Return the album.
                    return new Album(album.Name, artistID, album.Uri, album.Images[0].Url);
                }
            }

            return null;
        }

        private void RefreshSpotifyToken()
        {
            // Get spotify authorization token.
            ClientCredentialsAuth auth = new ClientCredentialsAuth()
            {
                ClientId = SpotifyAPIKeys.ClientID,
                ClientSecret = SpotifyAPIKeys.ClientSecret
            };
            Token token = auth.DoAuth();
            Session[Constants.ApolloTokenSession] = token;
        }
    }
}