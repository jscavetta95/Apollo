using Newtonsoft.Json;
using Apollo.Models.Spurify;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Apollo.Models;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Models;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;

namespace Apollo.Controllers
{
    public class SpurifyController : Controller {

        /*

        public ActionResult Index() {
            return View();
        }

        public ActionResult Display()
        {
            SpotifyWebAPI spotify = SpotifyAuth();
            if(spotify != null)
            {
                return View("Display", spotify.GetUserPlaylists(spotify.GetPrivateProfile().Id).Items);
            }
            else
            {
                return Redirect("Index");
            }
        }

        private SpotifyWebAPI SpotifyAuth()
        {
            WebAPIFactory webApiFactory = new WebAPIFactory(
                Url.Action("Display", "Spurify"),
                8000,
                SpotifyAPIModel.CLIENT_ID,
                Scope.PlaylistModifyPrivate,
                TimeSpan.FromSeconds(20));

            try
            {
                return webApiFactory.GetWebApi().Result;
            }
            catch (Exception)
            {
                return null;
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
                using (JsonReader reader = new JsonTextReader(new StreamReader(SpotifyAPILegacy.MakeAPICall(playListTracksURI, (SpotifyTokens)Session[TOKENS_SESSION])))) {

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
                SpotifyAPILegacy.RefreshTokens(((SpotifyTokens)Session[TOKENS_SESSION]));
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

        */
    }
}