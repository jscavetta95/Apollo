using Newtonsoft.Json;
using Spurify.Models.Spurify;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Spurify.Controllers
{
    public class SpurifyController : Controller {

        private struct SpotifyTokens {
            public string AccessToken;
            public string RefreshToken;
            public DateTime Expires;

            public SpotifyTokens(string accessToken, string refreshToken, DateTime expires) {
                AccessToken = accessToken;
                RefreshToken = refreshToken;
                Expires = expires;
            }

            public bool IsAccessValid() {
                return DateTime.Now < Expires;
            }
        }

        #region Constants

        private const string SPOTIFY_CLIENT_ID = "45c5e785d0644c94a41d23b43ebc9890";
        private const string SPOTIFY_CLIENT_SECRET = "301e67b7fa5a4b3bbd907a59b2942471";

        private const string REDIRECT = "http://localhost:58405/Spurify/Login";
        private const string SCOPE = "playlist-read-private%20playlist-read-collaborative%20playlist-modify-public%20playlist-modify-private";
        private const string SHOW_DIALOG = "true";

        private const string TOKEN_URI = "https://accounts.spotify.com/api/token";
        private const string PROFILE_URI = "https://api.spotify.com/v1/me";

        private const string LASTFM_API_KEY = "a4a88b715f00a2baeb78f629bf5787ef";
        private const string LASTFM_LIMIT = "200";

        private const string BLA = "hi :&)";

        public const string TOKENS_SESSION = "tokens";
        public const string PROGRESS_SESSION = "progress";
        public const string PLAYLISTID_SESSION = "playlistID";

        #endregion

        #region ActionResult Web Handlers

        public ActionResult Index() {
            Object AUTH_URI = $"https://accounts.spotify.com/authorize?client_id={SPOTIFY_CLIENT_ID}&response_type=code&redirect_uri={REDIRECT}&scope={SCOPE}&show_dialog={SHOW_DIALOG}";

            return View(AUTH_URI);
        }

        public ActionResult Display() {
            if(Session[TOKENS_SESSION] == null) {
                return Redirect("Index");
            }

            SpurifyViewModel viewModel = new SpurifyViewModel() {
                Playlists = GetPlaylists()
            };

            return View("Display", viewModel);
        }

        public ActionResult Login() {
            try {
                Session[TOKENS_SESSION] = GetTokens(Request.QueryString["code"]);
            } catch (NotImplementedException) {
                throw new Exception(Request.QueryString["error"]);
            }

            return Redirect("Display");
        }

        #endregion

        #region Methods

        private List<Playlist> GetPlaylists() {
            string PLAYLISTS_URI = $"https://api.spotify.com/v1/me/playlists";

            List<Playlist> playlists = new List<Playlist>();
            string href = string.Empty;
            string name = string.Empty;
            bool next = false;

            using (JsonReader reader = new JsonTextReader(new StreamReader(MakeAPICall(PLAYLISTS_URI)))) {
                while (reader.Read()) {

                    if (reader.TokenType.ToString().Equals("PropertyName") && reader.Value.ToString().Equals("collaborative")) {
                        next = false;

                        while (!next && reader.Read()) {

                            if (reader.TokenType.ToString().Equals("PropertyName") && reader.Value.ToString().Equals("href")) {
                                reader.Read();
                                href = (string)reader.Value;

                                while (!next && reader.Read()) {

                                    if (reader.TokenType.ToString().Equals("PropertyName") && reader.Value.ToString().Equals("name")) {
                                        reader.Read();
                                        name = (string)reader.Value;
                                        playlists.Add(new Playlist(href, name));
                                        next = true;
                                    }
                                }
                            }
                        }
                    }

                }
                return playlists;
            }
        }

        public void FillPlaylistTracks(Playlist playlist) {
            string playListTracksURI = $"{playlist.Href}/tracks";

            Track track = null;
            string name = string.Empty;
            string artist = string.Empty;
            string uri = string.Empty;
            string next = string.Empty;
            bool gotTrackInfo = false;
            bool finished = false;

            while (!finished) {
                using (JsonReader reader = new JsonTextReader(new StreamReader(MakeAPICall(playListTracksURI)))) {

                    while (!finished && reader.Read()) {

                        if (reader.TokenType.ToString().Equals("PropertyName") && reader.Value.ToString().Equals("artists")) {
                            gotTrackInfo = false;

                            while (!gotTrackInfo && reader.Read()) {

                                if (reader.TokenType.ToString().Equals("PropertyName") && reader.Value.ToString().Equals("artists")) {

                                    while (!gotTrackInfo && reader.Read()) {

                                        if (reader.TokenType.ToString().Equals("PropertyName") && reader.Value.ToString().Equals("name")) {
                                            reader.Read();
                                            artist = (string)reader.Value;

                                            while (!gotTrackInfo && reader.Read()) {

                                                if (reader.TokenType.ToString().Equals("PropertyName") && reader.Value.ToString().Equals("external_ids")) {

                                                    while (!gotTrackInfo && reader.Read()) {

                                                        if (reader.TokenType.ToString().Equals("PropertyName") && reader.Value.ToString().Equals("name")) {
                                                            reader.Read();
                                                            name = (string)reader.Value;

                                                            while (!gotTrackInfo && reader.Read()) {

                                                                if (reader.TokenType.ToString().Equals("PropertyName") && reader.Value.ToString().Equals("uri")) {
                                                                    reader.Read();
                                                                    uri = (string)reader.Value;
                                                                    track = new Track(name, artist, uri);
                                                                    playlist.Tracks.Add(track);
                                                                    gotTrackInfo = true;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                        } else if (reader.TokenType.ToString().Equals("PropertyName") && reader.Value.ToString().Equals("next")) {
                            reader.Read();
                            next = (string)reader.Value;

                            if (next != null) {
                                playListTracksURI = $"{next}";
                                System.Threading.Thread.Sleep(500);
                            } else {
                                finished = true;
                            }
                        }
                    }
                }
            }
        }

        public JsonResult DeleteTrackFromPlaylist(string trackUri) {
            string uri = $"https://api.spotify.com/v1/users/{GetCurrentUser()}/playlists/{Session[PLAYLISTID_SESSION]}/tracks";

            if (!((SpotifyTokens)Session[TOKENS_SESSION]).IsAccessValid()) {
                Session[TOKENS_SESSION] = RefreshTokens();
            }

            using (HttpClient httpClient = new HttpClient()) {
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + ((SpotifyTokens)Session[TOKENS_SESSION]).AccessToken);

                HttpRequestMessage request = new HttpRequestMessage {
                    Content = new StringContent(@"{ ""tracks"": [{ ""uri"": " + "\"" + trackUri + "\"" + @" }] }", Encoding.UTF8, "application/json"),
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri(uri)
                };
                HttpResponseMessage response = httpClient.SendAsync(request).Result;

                response.EnsureSuccessStatusCode();
                return Json(new { result = true });
            }
        }

        private string GetCurrentUser() {
            string userID = string.Empty;

            using (JsonReader reader = new JsonTextReader(new StreamReader(MakeAPICall(PROFILE_URI)))) {
                bool finished = false;
                while (!finished && reader.Read()) {
                    if (reader.TokenType.ToString().Equals("PropertyName") && reader.Value.ToString().Equals("id")) {
                        reader.Read();
                        userID = (string)reader.Value;
                        finished = true;
                    }
                }
                return userID;
            }
        }

        #endregion

        #region Spotify API

        private Stream MakeAPICall(string URI) {
            if (!((SpotifyTokens)Session[TOKENS_SESSION]).IsAccessValid()) {
                Session[TOKENS_SESSION] = RefreshTokens();
            }

            HttpResponseMessage response;
            using (HttpClient httpClient = new HttpClient()) {
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + ((SpotifyTokens)Session[TOKENS_SESSION]).AccessToken);
                response = httpClient.GetAsync(URI).Result;
                response.EnsureSuccessStatusCode();
            }

            return response.Content.ReadAsStreamAsync().Result;
        }

        private SpotifyTokens RefreshTokens() {
            return GetTokens();
        }

        private SpotifyTokens GetTokens(string authCode = null) {
            string accessToken = string.Empty;
            string refreshToken = string.Empty;
            DateTime expires = DateTime.MinValue;

            using (HttpClient httpClient = new HttpClient()) {
                Dictionary<string, string> parameters;

                if (authCode != null) {

                    parameters = new Dictionary<string, string> {
                            { "grant_type", "authorization_code" },
                            { "code", authCode },
                            { "redirect_uri", REDIRECT },
                            { "client_id", SPOTIFY_CLIENT_ID },
                            { "client_secret", SPOTIFY_CLIENT_SECRET }
                    };

                } else {

                    parameters = new Dictionary<string, string> {
                        { "grant_type", "refresh_token" },
                        { "refresh_token ", ((SpotifyTokens)Session[TOKENS_SESSION]).RefreshToken }
                    };
                    httpClient.DefaultRequestHeaders.Add("Authorization", Convert.ToBase64String(Encoding.ASCII.GetBytes(SPOTIFY_CLIENT_ID + ":" + SPOTIFY_CLIENT_SECRET)));

                }

                HttpResponseMessage response = httpClient.PostAsync(TOKEN_URI, new FormUrlEncodedContent(parameters)).Result;

                if (response.StatusCode == HttpStatusCode.OK) {
                    using (JsonReader reader = new JsonTextReader(new StreamReader(response.Content.ReadAsStreamAsync().Result))) {
                        while (reader.Read()) {
                            if (reader.TokenType.ToString().Equals("PropertyName") && reader.Value.ToString().Equals("access_token")) {
                                reader.Read();
                                accessToken = (string)reader.Value;
                            } else if (reader.TokenType.ToString().Equals("PropertyName") && reader.Value.ToString().Equals("refresh_token")) {
                                reader.Read();
                                refreshToken = (string)reader.Value;
                            } else if (reader.TokenType.ToString().Equals("PropertyName") && reader.Value.ToString().Equals("expires_in")) {
                                reader.Read();
                                expires = DateTime.Now + TimeSpan.FromSeconds(Double.Parse(reader.Value.ToString()));
                            }
                        }
                    }
                } else {
                    throw new HttpException(response.StatusCode.ToString());
                }
            }
            return new SpotifyTokens(accessToken, refreshToken, expires);
        }

        #endregion

        #region Last.fm API

        public JsonResult GetPlaycountsForPlaylistFromLastFM(string user, string playlistJSON) {
            Playlist playlist = JsonConvert.DeserializeObject<Playlist>(playlistJSON);
            FillPlaylistTracks(playlist);

            string trackName;
            string artist;
            string uri;

            bool done;

            using (HttpClient httpClient = new HttpClient()) {
                foreach(Track track in playlist.Tracks) {
                    trackName = track.Name.Replace(" ", "%20");
                    artist = track.Artist.Replace(" ", "%20");

                    uri = $"http://ws.audioscrobbler.com/2.0/?method=track.getInfo&track={trackName}&artist={artist}&username={user}&autocorrect=1&api_key={LASTFM_API_KEY}&format=json";

                    using (JsonReader reader = new JsonTextReader(new StreamReader(httpClient.GetStreamAsync(uri).Result))) {
                        done = false;
                        while (!done && reader.Read()) {
                            if (reader.TokenType.ToString().Equals("PropertyName") && reader.Value.ToString().Equals("userplaycount")) {
                                reader.Read();
                                track.PlayCount = int.Parse(reader.Value.ToString());
                                done = true;
                            }
                        }
                    }
                }
            }

            Session[PLAYLISTID_SESSION] = playlist.GetID();
            return Json(new { tracks = playlist.Tracks } , JsonRequestBehavior.AllowGet);
        }

        #endregion

    }
}