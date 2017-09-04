using Newtonsoft.Json;
using Apollo.Models.Spurify;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Apollo.Controllers
{
    public class SpurifyController : Controller {

        #region Constants

        private const string LASTFM_API_KEY = "a4a88b715f00a2baeb78f629bf5787ef";
        private const string LASTFM_LIMIT = "200";

        public const string TOKENS_SESSION = "tokens";
        public const string PROGRESS_SESSION = "progress";
        public const string PLAYLISTID_SESSION = "playlistID";

        #endregion

        #region ActionResult Web Handlers

        public ActionResult Index() {
            return View(SpotifyAPI.AUTH_URL);
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
                SpotifyTokens tokens = new SpotifyTokens();
                SpotifyAPI.GetTokens(tokens, Request.QueryString["code"]);
                Session[TOKENS_SESSION] = tokens;
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

            using (JsonReader reader = new JsonTextReader(new StreamReader(SpotifyAPI.MakeAPICall(PLAYLISTS_URI, (SpotifyTokens)Session[TOKENS_SESSION])))) {
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
                using (JsonReader reader = new JsonTextReader(new StreamReader(SpotifyAPI.MakeAPICall(playListTracksURI, (SpotifyTokens)Session[TOKENS_SESSION])))) {

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
                SpotifyAPI.RefreshTokens(((SpotifyTokens)Session[TOKENS_SESSION]));
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

            using (JsonReader reader = new JsonTextReader(new StreamReader(SpotifyAPI.MakeAPICall(SpotifyAPI.PROFILE_URI, (SpotifyTokens)Session[TOKENS_SESSION])))) {
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

        #region Last.fm API

        public JsonResult GetPlaycountsForPlaylistFromLastFM(string user, string playlistJSON) {
            Playlist playlist = JsonConvert.DeserializeObject<Playlist>(playlistJSON);
            FillPlaylistTracks(playlist);

            string trackName;
            string artist;
            string uri;

            bool done;

            using (HttpClient httpClient = new HttpClient()) {

                foreach (Track track in playlist.Tracks) {
                    trackName = HttpUtility.UrlEncode(track.Name);
                    artist = HttpUtility.UrlEncode(track.Artist);

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