using Newtonsoft.Json;
using Apollo.Models.Apollo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Mvc;


namespace Apollo.Legacy {
    public class SpotifyAPILegacy {

        #region Constants

        public const string SPOTIFY_CLIENT_ID = "45c5e785d0644c94a41d23b43ebc9890";
        private const string SPOTIFY_CLIENT_SECRET = "301e67b7fa5a4b3bbd907a59b2942471";

        public const string REDIRECT = "http://localhost:58405/Spurify/Login";
        public const string SCOPE = "playlist-read-private%20playlist-read-collaborative%20playlist-modify-public%20playlist-modify-private";
        public const string SHOW_DIALOG = "true";

        private const string TOKEN_URI = "https://accounts.spotify.com/api/token";
        public const string PROFILE_URI = "https://api.spotify.com/v1/me";

        #endregion

        public static string AUTH_URL {
            get {
                return $"https://accounts.spotify.com/authorize?client_id={SPOTIFY_CLIENT_ID}&response_type=code&redirect_uri={REDIRECT}&scope={SCOPE}&show_dialog={SHOW_DIALOG}";
            }
        }

        public static Stream MakeAPICall(string URI, SpotifyTokens tokens) {
            if (!tokens.IsAccessValid()) {
                RefreshTokens(tokens);
            }

            HttpResponseMessage response;
            using (HttpClient httpClient = new HttpClient()) {
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + tokens.AccessToken);
                response = httpClient.GetAsync(URI).Result;
                response.EnsureSuccessStatusCode();
            }

            return response.Content.ReadAsStreamAsync().Result;
        }

        public static void RefreshTokens(SpotifyTokens tokens) {
            GetTokens(tokens);
        }

        public static void GetTokens(SpotifyTokens tokens, string authCode = null) {
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
                        { "refresh_token ", tokens.RefreshToken }
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
            tokens.AccessToken = accessToken;
            tokens.RefreshToken = refreshToken;
            tokens.Expires = expires;
        }

        public static string GetAccessTokenClientCredentialFlow() {
            using (HttpClient httpClient = new HttpClient()) {
                Dictionary<string, string> parameters = new Dictionary<string, string> {
                    { "grant_type", "client_credentials" },
                };
                // TODO: headers not working.
                httpClient.DefaultRequestHeaders.Add("Authorization", Convert.ToBase64String(Encoding.ASCII.GetBytes(SPOTIFY_CLIENT_ID + ":" + SPOTIFY_CLIENT_SECRET)));
                HttpResponseMessage response = httpClient.PostAsync(TOKEN_URI, new FormUrlEncodedContent(parameters)).Result;
                using (JsonReader reader = new JsonTextReader(new StreamReader(response.Content.ReadAsStreamAsync().Result))) {
                    while (reader.Read()) {
                        if (reader.TokenType.ToString().Equals("PropertyName") && reader.Value.ToString().Equals("access_token")) {
                            reader.Read();
                            return (string)reader.Value;
                        }
                    }
                }
            }
            return null;
        }

        public static List<Album> GetArtistAlbums(string artistID) {
            string url = $"https://api.spotify.com/v1/artists/{artistID}/albums?album_type=album";
            return null;
        }

        public static List<string> GetRelatedArtistIds(string artist, string accessToken = null) {
            if(accessToken == null) {
                accessToken = GetAccessTokenClientCredentialFlow();
            }
            string url = $"https://api.spotify.com/v1/artists/{artist}/related-artists";

            HttpResponseMessage response;
            using (HttpClient httpClient = new HttpClient()) {
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                response = httpClient.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
            }

            var result = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
            List<string> relatedArtists = new List<string>();
            

            return relatedArtists;
        }

    }

    public struct SpotifyTokens {
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

}