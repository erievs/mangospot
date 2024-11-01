using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MangoSpot
{
    public class TokenManager
    {
        private const string TokenUrl = "https://accounts.spotify.com/api/token";

        public async Task<string> RefreshAccessTokenAsync()
        {
            using (var httpClient = new HttpClient())
            {

                if (string.IsNullOrEmpty(Settings.RefreshToken))
                {
                    System.Diagnostics.Debug.WriteLine("Refresh token is null or empty.");
                    return null;
                }

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, TokenUrl);

                var body = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", Settings.RefreshToken),
                    new KeyValuePair<string, string>("client_id", Settings.ClientID),
                    new KeyValuePair<string, string>("client_secret", Settings.ClientSecret)
                });

                tokenRequest.Content = body;

                System.Diagnostics.Debug.WriteLine("Sending refresh token request to Spotify API:");
                System.Diagnostics.Debug.WriteLine($"URL: {tokenRequest.RequestUri}");
                System.Diagnostics.Debug.WriteLine($"Body: {await body.ReadAsStringAsync()}");

                try
                {
                    var response = await httpClient.SendAsync(tokenRequest);
                    System.Diagnostics.Debug.WriteLine($"Response Status Code: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine("Response JSON:");
                        System.Diagnostics.Debug.WriteLine(jsonResponse);

                        var tokenData = JObject.Parse(jsonResponse);

                        Settings.AccessToken = tokenData["access_token"]?.ToString();

                        System.Diagnostics.Debug.WriteLine("Access token refreshed successfully.");
                        return Settings.AccessToken;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"Error refreshing access token: {response.ReasonPhrase}");
                        System.Diagnostics.Debug.WriteLine($"Response Content: {errorContent}");
                        return null;
                    }
                }
                catch (HttpRequestException e)
                {
                    System.Diagnostics.Debug.WriteLine($"Request error: {e.Message}");
                    return null;
                }
                catch (JsonException jsonEx)
                {
                    System.Diagnostics.Debug.WriteLine($"JSON error: {jsonEx.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Unexpected error: {ex.Message}");
                    return null;
                }
            }
        }

    }


        public class TokenResponse
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
            public string TokenType { get; set; }
            public int ExpiresIn { get; set; }
        }
}
