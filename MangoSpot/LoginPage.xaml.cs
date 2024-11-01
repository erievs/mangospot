using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Networking.Connectivity;


namespace MangoSpot
{
    public sealed partial class LoginPage : Page
    {

        private LocalWebServer server;

        public LoginPage()
        {
            this.InitializeComponent();
            LoadSettings();
            DisplayLocalIPAddress();
        }

        private void LoadSettings()
        {
            ClientIDTextBox.Text = Settings.ClientID;
            ClientSecretTextBox.Text = Settings.ClientSecret;
            RedirectUriTextBox.Text = Settings.RedirectUri;
        }

        private void DisplayLocalIPAddress()
        {
            string localIpAddress = GetLocalIPAddress();
            IpAddressTextBlock.Text = $"Local IP Address: {localIpAddress}:3000";
        }


        public string GetLocalIPAddress()
        {
            var hostNames = NetworkInformation.GetHostNames();

            foreach (var hostName in hostNames)
            {
                if (hostName.IPInformation != null && hostName.Type == Windows.Networking.HostNameType.Ipv4)
                {
                    string ipAddress = hostName.CanonicalName;

                    string[] ipParts = ipAddress.Split('.');

                    if (ipParts.Length == 4)
                    {

                        return ipAddress;
                    }
                }
            }

            return "No valid IP Address found.";
        }

        private async void StartServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (LocalWebServer.IsRunning == false)
            {
                server = new LocalWebServer(3000);
                await server.Start(3000);
                server.ParametersSet += UpdateUIWithSettings;
                string localIpAddress = GetLocalIPAddress();
            }
        }

        public async void UpdateUIWithSettings()
        {

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
             {
                 ClientIDTextBox.Text = Settings.ClientID;
                 ClientSecretTextBox.Text = Settings.ClientSecret;
                 RedirectUriTextBox.Text = Settings.RedirectUri;
                 AccessTokenTextBox.Text = Settings.AccessToken;
             });
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.ClientID = ClientIDTextBox.Text;
            Settings.ClientSecret = ClientSecretTextBox.Text;
            Settings.RedirectUri = ClientSecretTextBox.Text;
            AuthenticateUser();
        }

        private void AuthenticateUser()
        {
            string authUrl = GetAuthorizationUrl();
            System.Diagnostics.Debug.WriteLine($"Navigating to: {authUrl}");
            GenerateQRCode(authUrl);
            QRCodeImage.Visibility = Visibility.Visible;
        }

        private string GetAuthorizationUrl()
        {
            string clientId = Settings.ClientID;
            string redirectUri = Uri.EscapeDataString("http://localhost:3000/callback");

            string scope = Uri.EscapeDataString("user-read-private playlist-read-private user-library-read user-library-modify playlist-modify-private playlist-modify-public user-read-recently-played user-modify-playback-state user-read-playback-state");

            string authEndpoint = "https://accounts.spotify.com/authorize";

            return $"{authEndpoint}?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope={scope}";
        }

        private async void GenerateQRCode(string text)
        {
            using (QRCodeData qrCodeData = QRCodeGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q))
            {
                using (BitmapByteQRCode qrCode = new BitmapByteQRCode(qrCodeData))
                {
                    byte[] qrCodeImage = qrCode.GetGraphic(20);

                    using (var stream = new InMemoryRandomAccessStream())
                    {
                        using (var writer = new DataWriter(stream))
                        {
                            writer.WriteBytes(qrCodeImage);
                            await writer.StoreAsync();
                            writer.DetachStream();
                        }

                        stream.Seek(0);
                        BitmapImage bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(stream);
                        QRCodeImage.Source = bitmapImage;
                    }
                }
            }
        }

        private void ImportAuthStuffeFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            openPicker.FileTypeFilter.Add(".csv");

            openPicker.PickSingleFileAndContinue();
        }

        public async void ImportFile(StorageFile file)
        {
            await ReadFileContent(file);
        }

        private async Task ReadFileContent(StorageFile file)
        {
            try
            {
                string content = await FileIO.ReadTextAsync(file);
                string[] values = content.Split(',');

                if (content.Contains("?code="))
                {
                    string code = ExtractCode(content);
                    Debug.WriteLine($"Extracted Code: {code}");
                    AccessTokenTextBox.Text = code;
                }

                else if (values.Length == 3)
                {
                    Settings.ClientID = values[0].Trim();
                    Settings.ClientSecret = values[1].Trim();
                    Settings.RedirectUri = values[2].Trim();

                    ClientIDTextBox.Text = Settings.ClientID;
                    ClientSecretTextBox.Text = Settings.ClientSecret;
                    RedirectUriTextBox.Text = Settings.RedirectUri;

                    Debug.WriteLine($"Imported Auth Details - ClientID: {Settings.ClientID}, ClientSecret: {Settings.ClientSecret}, RedirectURI: {Settings.RedirectUri}");
                }
                else
                {
                    var dialog = new MessageDialog("Invalid file format. Please ensure the content is correctly formatted.");
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog($"An error occurred while reading the file: {ex.Message}");
                await dialog.ShowAsync();
            }
        }

        private string ExtractCode(string content)
        {
            string code = string.Empty;
            if (content.Contains("callback?code="))
            {

                code = content.Split(new[] { "callback?code=" }, StringSplitOptions.None)[1].Split('&')[0];

                Debug.WriteLine($"Raw Content: {content}");
                Debug.WriteLine($"Extracted Code: {code}");
            }
            return code;
        }

        private void SubmitCodeButton_Click(object sender, RoutedEventArgs e)
        {
            string accessCode = AccessTokenTextBox.Text;
            if (!string.IsNullOrEmpty(accessCode))
            {
                ProcessAccessCode(accessCode);
            }
        }

        private async void ProcessAccessCode(string accessCode)
        {
            System.Diagnostics.Debug.WriteLine($"Extracted Access Code: {accessCode}");

            string jsonResponse = await GetAccessTokenAsync(accessCode);

            System.Diagnostics.Debug.WriteLine($"JSON Response: {jsonResponse}");

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                try
                {

                    var tokenData = JObject.Parse(jsonResponse);

                    string accessToken = tokenData["access_token"]?.ToString();
                    string refreshToken = tokenData["refresh_token"]?.ToString();

                    if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
                    {

                        Settings.AccessToken = accessToken;
                        Settings.RefreshToken = refreshToken;

                        bool isValid = await ValidateAccessCodeAsync(accessToken);
                        if (isValid)
                        {
                            var dialog = new MessageDialog("Access Token has been set successfully.");
                            await dialog.ShowAsync();

                            Frame.Navigate(typeof(MainPage));
                        }
                        else
                        {
                            var errorDialog = new MessageDialog("Access Token is invalid. Please try again.");
                            await errorDialog.ShowAsync();
                        }
                    }
                    else
                    {
                        var errorDialog = new MessageDialog("Failed to retrieve Access Token. Please try again. It may have expired!");
                        await errorDialog.ShowAsync();
                    }
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"JSON Parsing Error: {ex.Message}");
                    var errorDialog = new MessageDialog("Error parsing the token response.");
                    await errorDialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Unexpected error: {ex.Message}");
                    var errorDialog = new MessageDialog("An unexpected error occurred.");
                    await errorDialog.ShowAsync();
                }
            }
            else
            {
                var errorDialog = new MessageDialog("Failed to retrieve Access Token. Please try again.");
                await errorDialog.ShowAsync();
            }
        }

        private async Task<string> GetAccessTokenAsync(string authorizationCode)
        {
            using (var client = new HttpClient())
            {

                string clientId = Settings.ClientID;
                string clientSecret = Settings.ClientSecret;

                string redirectUri = "http://localhost:3000/callback";

                var tokenRequestBody = new FormUrlEncodedContent(new Dictionary<string, string>

                {
                    { "grant_type", "authorization_code" },
                    { "code", authorizationCode},
                    { "redirect_uri", redirectUri },
                    { "client_id", clientId },
                    { "client_secret", clientSecret }
                });

                var bodyString = await tokenRequestBody.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Token Request Body: {bodyString}");

                try
                {
                    var response = await client.PostAsync("https://accounts.spotify.com/api/token", tokenRequestBody);

                    System.Diagnostics.Debug.WriteLine($"Response Status Code: {response.StatusCode}");

                    var responseContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Response Content: {responseContent}");

                    if (!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error Response: {responseContent}");
                        return null;
                    }

                    return responseContent;
                }
                catch (HttpRequestException e)
                {
                    System.Diagnostics.Debug.WriteLine($"Request error: {e.Message}");
                    return null;
                }
                catch (JsonException e)
                {
                    System.Diagnostics.Debug.WriteLine($"JSON error: {e.Message}");
                    return null;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine($"Unexpected error: {e.Message}");
                    return null;
                }
            }
        }

        private async Task<string> RefreshAccessTokenAsync()
        {

            if (string.IsNullOrEmpty(Settings.RefreshToken))
            {
                System.Diagnostics.Debug.WriteLine("Refresh token is null or empty.");
                return null;
            }

            using (var httpClient = new HttpClient())
            {

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");

                var body = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", Settings.RefreshToken),
                    new KeyValuePair<string, string>("client_id", Settings.ClientID),
                    new KeyValuePair<string, string>("client_secret", Settings.ClientSecret)
                });

                tokenRequest.Content = body;

                try
                {

                    var response = await httpClient.SendAsync(tokenRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();

                        var tokenData = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenResponse>(jsonResponse);
                        Settings.AccessToken = tokenData.AccessToken;

                        System.Diagnostics.Debug.WriteLine("Access token refreshed successfully.");
                        return tokenData.AccessToken;
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

        private async Task<bool> ValidateAccessCodeAsync(string accessToken)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {

                    var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    System.Diagnostics.Debug.WriteLine("Sending request to Spotify API:");
                    System.Diagnostics.Debug.WriteLine($"URL: {request.RequestUri}");
                    System.Diagnostics.Debug.WriteLine($"Authorization Header: {request.Headers.Authorization}");

                    var response = await httpClient.SendAsync(request);

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {

                        string newAccessToken = await RefreshAccessTokenAsync();
                        if (!string.IsNullOrEmpty(newAccessToken))
                        {

                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
                            response = await httpClient.SendAsync(request);
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"Response Status: {response.StatusCode}");

                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating access code: {ex.Message}");
                return false;
            }
        }

        public class TokenResponse
        {
            public string AccessToken { get; set; }
            public string TokenType { get; set; }
            public int ExpiresIn { get; set; }
            public string RefreshToken { get; set; }
            public string Scope { get; set; }
        }

    }
}