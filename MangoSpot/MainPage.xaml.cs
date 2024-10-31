﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media.Animation;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;
using Windows.UI.Xaml.Input;
using Windows.Data.Json;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.Media.Playback;
using Windows.Foundation.Collections;
using Windows.Foundation;

namespace MangoSpot
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            _playlists = new ObservableCollection<Playlist>();
            _liked = new ObservableCollection<Track>();
            _recentlyPlayed = new ObservableCollection<Track>();
            _recommendations = new ObservableCollection<Track>();
            _searchResults = new ObservableCollection<Track>();
            PlaylistsListView.ItemsSource = _playlists;
            HistoryListView.ItemsSource = _recentlyPlayed;
            LikedSongsListView.ItemsSource = _liked;
            RecommendationsListView.ItemsSource = _recommendations;

        }

        private int _offset = 0;
        private const int _limit = 50; 
        private bool _isLoading = false;
        private ObservableCollection<Playlist> _playlists;
        private ObservableCollection<Track> _recentlyPlayed;
        private ObservableCollection<Track> _recommendations;
        private ObservableCollection<Track> _liked;
        private ObservableCollection<Track> _searchResults;

        private int _currentTrackIndex = -1;
        private string _currentListType; 


        private bool isPlaying = false;
        private int _currentOffset = 0;

        private string currentSongID = "";

        private List<Track> _currentPlaylist;

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {

            ContentDialog aboutDialog = new ContentDialog
            {
                Title = "MangoSpot",
                Content = new TextBlock
                {
                    Text = "Version: Beta 1.0.0" + Environment.NewLine +
                           "\nCredits:" + Environment.NewLine +
                           "\nNCP3.0" + Environment.NewLine +
                           "\nKierownik (Backgrounds)" + Environment.NewLine +
                            "\nErie Valley Software 2022-2024",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 10, 0, 10)
                },
            };

            await aboutDialog.ShowAsync();
        }

        private async void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var pivot = sender as Pivot;
            var selectedItem = pivot.SelectedItem as PivotItem;

            FadeOutStoryboard.Begin();

            await Task.Delay(500); 

            switch (selectedItem.Tag.ToString())
            {
                case "account":
                    await LoadAccountDataAsync();
                    BackgroundImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/Bridge 2.jpg"));
                    break;

                case "liked":
                    await LoadLikedSongsAsync();
                    BackgroundImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/Sea.jpg"));
                    break;

                case "playlists": 
                    await LoadPlaylistsAsync();
                    BackgroundImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/Mineshaft 2.jpg"));
                    break;

                case "history":
                    await LoadRecentlyPlayedTracksAsync();
                    BackgroundImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/Castle.jpg"));
                    break;

                case "recommendations":
                    await LoadRecommendationsAsync();
                    BackgroundImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/Sione 2.jpg"));
                    break;

                default:
                    break;
            }

            FadeInStoryboard.Begin();
        }

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.Trim();

            if (!string.IsNullOrEmpty(searchText))
            {
                await SearchTracksAsync(searchText, true); 
            }
            else
            {
                _searchResults.Clear();
                SearchResultsListView.ItemsSource = null;
            }
        }

        private void Search_ScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null && scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight)
            {
                string searchText = SearchTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(searchText))
                {
                    SearchTracksAsync(searchText, false); 
                }
            }
        }

        private async void SearchResultsListView_ItemClick(object sender, ItemClickEventArgs e)  {

        var selectedTrack = e.ClickedItem as Track;

            if (selectedTrack != null)
            {

                foreach (var track in _searchResults)
                {
                    track.IsSelected = false;
                }

                selectedTrack.IsSelected = true;

                string trackName = selectedTrack.Name;
                string artistName = selectedTrack.Artist;
                string spotifyTrackId = selectedTrack.SpotifyTrackId;

                System.Diagnostics.Debug.WriteLine($"Searched Track clicked: {trackName} by {artistName}");

                await PlayTrackAsync(trackName, artistName, spotifyTrackId);
            }
        }

        private async Task LikeSongAsync(string trackId)
        {
            string accessToken = Settings.AccessToken;

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Put, "https://api.spotify.com/v1/me/tracks?ids=" + trackId);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    var response = await httpClient.SendAsync(request);

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine("Response JSON: " + jsonResponse);

                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine("Song liked successfully.");
                        await ShowPopupAsync("Song liked successfully!");
                    }
                    else
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            if (!await RefreshTokenAsync(httpClient, request))
                            {
                                await ShowPopupAsync("Failed to refresh access token.");
                            }
                            else
                            {
                                await LikeSongAsync(trackId);
                            }
                        }
                        else
                        {
                            await ShowPopupAsync("Failed to like song.");
                        }
                    }
                }
                catch (InvalidOperationException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"InvalidOperationException occurred: {ex.Message}");
                    await ShowPopupAsync("An error occurred while liking the song. Please try again.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"An unexpected error occurred: {ex.Message}");
                    await ShowPopupAsync("An unexpected error occurred. Please try again.");
                }
            }
        }

        private async Task SearchTracksAsync(string query, bool isNewSearch = true)
        {
            if (_isLoading)
            {
                System.Diagnostics.Debug.WriteLine("SearchTracksAsync: Already loading, returning early.");
                return;
            }

            _isLoading = true;
            System.Diagnostics.Debug.WriteLine("SearchTracksAsync: Starting to load search results.");

            string accessToken = Settings.AccessToken;
            var requestUri = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track&limit={_limit}&offset={_currentOffset}";

            using (var httpClient = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                try
                {
                    var response = await httpClient.SendAsync(request);
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"SearchTracksAsync: Response Status Code: {(int)response.StatusCode} {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine("SearchTracksAsync: JSON Response: " + jsonResponse);

                    if (response.IsSuccessStatusCode)
                    {
                        var tracksData = JObject.Parse(jsonResponse)["tracks"]["items"];
                        System.Diagnostics.Debug.WriteLine($"SearchTracksAsync: Found {tracksData?.Count()} tracks.");

                        if (isNewSearch)
                        {
                            _searchResults.Clear();
                            _currentOffset = 0;
                        }

                        foreach (var item in tracksData)
                        {
                            var track = new Track
                            {
                                Name = item["name"]?.ToString(),
                                Artist = item["artists"]?[0]?["name"]?.ToString(),
                                SpotifyTrackId = item["id"]?.ToString(),
                            };

                            _searchResults.Add(track);
                        }

                        SearchResultsListView.ItemsSource = _searchResults;
                        System.Diagnostics.Debug.WriteLine("SearchTracksAsync: Search results loaded successfully.");

                        _currentOffset += tracksData.Count();
                    }
                    else
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            if (!await RefreshTokenAsync(httpClient, request))
                            {
                                await ShowPopupAsync("Failed to refresh access token.");
                            }
                            else
                            {
                                await LoadAccountDataAsync();
                            }
                        }
                        else
                        {
                            await ShowPopupAsync("Failed to load user data.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("SearchTracksAsync: Exception occurred: " + ex.Message);
                    await ShowPopupAsync("An error occurred while searching for tracks.");
                }
            }

            _isLoading = false;
        }

        private async Task LoadRecommendationsAsync()
        {
            if (_isLoading)
            {
                System.Diagnostics.Debug.WriteLine("LoadRecommendationsAsync: Already loading, returning early.");
                return;
            }

            _isLoading = true;
            System.Diagnostics.Debug.WriteLine("LoadRecommendationsAsync: Starting to load recommendations.");

            string accessToken = Settings.AccessToken;

            string seedTracks = "0c6xIDDpzE81m2q797ordA";
            string seedArtists = "4NHQUGzhtTLFvgF5SZesLK";
            string seedGenres = "country,pop";

            var requestUri = $"https://api.spotify.com/v1/recommendations?seed_tracks={seedTracks}&seed_artists={seedArtists}&seed_genres={seedGenres}&limit=100";

            using (var httpClient = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                try
                {
                    var response = await httpClient.SendAsync(request);
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"LoadRecommendationsAsync: Response Status Code: {(int)response.StatusCode} {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine("LoadRecommendationsAsync: JSON Response: " + jsonResponse);

                    if (response.IsSuccessStatusCode)
                    {
                        var recommendationsData = JObject.Parse(jsonResponse)["tracks"];
                        System.Diagnostics.Debug.WriteLine($"LoadRecommendationsAsync: Found {recommendationsData?.Count()} recommendations.");

                        _recommendations.Clear();

                        foreach (var item in recommendationsData)
                        {
                            var track = new Track
                            {
                                Name = item["name"]?.ToString(),
                                Artist = item["artists"]?[0]?["name"]?.ToString(),
                                SpotifyTrackId = item["id"]?.ToString(),
                            };

                            _recommendations.Add(track);
                        }

                        RecommendationsListView.ItemsSource = _recommendations;
                        System.Diagnostics.Debug.WriteLine("LoadRecommendationsAsync: Recommendations loaded successfully.");
                    }
                    else
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            if (!await RefreshTokenAsync(httpClient, request))
                            {
                                await ShowPopupAsync("Failed to refresh access token.");
                            }
                            else
                            {
                                await LoadAccountDataAsync();
                            }
                        }
                        else
                        {
                            await ShowPopupAsync("Failed to load user data.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("LoadRecommendationsAsync: Exception occurred: " + ex.Message);
                    await ShowPopupAsync("An error occurred while loading recommendations.");
                }
            }

            _isLoading = false;
        }

        private async Task LoadRecentlyPlayedTracksAsync()
        {
            if (_isLoading)
            {
                System.Diagnostics.Debug.WriteLine("LoadRecentlyPlayedTracksAsync: Already loading, returning early.");
                return;
            }

            _isLoading = true;
            System.Diagnostics.Debug.WriteLine("LoadRecentlyPlayedTracksAsync: Starting to load recently played tracks.");

            string accessToken = Settings.AccessToken;
            using (var httpClient = new HttpClient())
            {
                var requestUri = $"https://api.spotify.com/v1/me/player/recently-played?limit={_limit}";
                var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                try
                {
                    var response = await httpClient.SendAsync(request);

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"LoadRecentlyPlayedTracksAsync: Response Status Code: {(int)response.StatusCode} {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine("LoadRecentlyPlayedTracksAsync: JSON Response: " + jsonResponse);

                    if (response.IsSuccessStatusCode)
                    {

                        var formattedJson = JToken.Parse(jsonResponse).ToString(Newtonsoft.Json.Formatting.Indented);
                        System.Diagnostics.Debug.WriteLine("LoadRecentlyPlayedTracksAsync: Formatted JSON Response:\n" + formattedJson);

                        var recentlyPlayedData = JObject.Parse(jsonResponse);
                        var tracks = recentlyPlayedData["items"];
                        System.Diagnostics.Debug.WriteLine($"LoadRecentlyPlayedTracksAsync: Found {tracks?.Count()} recently played tracks.");

                        _recentlyPlayed.Clear();

                        foreach (var item in tracks)
                        {
                            var trackInfo = item["track"];
                            if (trackInfo != null)
                            {
                                var track = new Track
                                {
                                    Name = trackInfo["name"]?.ToString(),
                                    Artist = trackInfo["artists"]?[0]?["name"]?.ToString(),
                                    SpotifyTrackId = trackInfo["id"]?.ToString()
                                };

                                _recentlyPlayed.Add(track);
                            }
                        }

                        System.Diagnostics.Debug.WriteLine("LoadRecentlyPlayedTracksAsync: Recently played tracks loaded successfully.");
                    }
                    else
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            if (!await RefreshTokenAsync(httpClient, request))
                            {
                                await ShowPopupAsync("Failed to refresh access token.");
                            }
                            else
                            {
                                await LoadAccountDataAsync();
                            }
                        }
                        else
                        {
                            await ShowPopupAsync("Failed to load user data.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("LoadRecentlyPlayedTracksAsync: Exception occurred: " + ex.Message);
                    await ShowPopupAsync("An error occurred while loading recently played tracks.");
                }
            }

            _isLoading = false;
        }

        private async void HistoryListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selectedTrack = e.ClickedItem as Track;

            if (selectedTrack != null)
            {
                string trackName = selectedTrack.Name;
                string artistName = selectedTrack.Artist;
                string spotifyTrackId = selectedTrack.SpotifyTrackId;

                _currentTrackIndex = _recentlyPlayed.IndexOf(selectedTrack);
                _currentListType = "History"; 

                System.Diagnostics.Debug.WriteLine($"Recently Played track clicked: {trackName} by {artistName}");

                await PlayTrackAsync(trackName, artistName, spotifyTrackId);
            }
        }

        private async Task LoadLikedSongsAsync()
        {
            if (_isLoading)
            {
                System.Diagnostics.Debug.WriteLine("LoadLikedSongsAsync: Already loading, returning early.");
                return;
            }

            _isLoading = true;
            System.Diagnostics.Debug.WriteLine("LoadLikedSongsAsync: Starting to load liked songs.");

            string accessToken = Settings.AccessToken;
            System.Diagnostics.Debug.WriteLine("LoadLikedSongsAsync: Access token retrieved: " + accessToken);

            using (var httpClient = new HttpClient())
            {
                var requestUri = $"https://api.spotify.com/v1/me/tracks?offset={_offset}&limit={_limit}";
                var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                System.Diagnostics.Debug.WriteLine("LoadLikedSongsAsync: Sending request to " + requestUri);

                try
                {
                    var response = await httpClient.SendAsync(request);
                    System.Diagnostics.Debug.WriteLine("LoadLikedSongsAsync: Received response with status code " + response.StatusCode);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine("LoadLikedSongsAsync: Error response content: " + errorContent);
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine("LoadLikedSongsAsync: Liked Songs JSON Response: " + jsonResponse);

                        var likedSongsData = JObject.Parse(jsonResponse);
                        var tracks = likedSongsData["items"];
                        System.Diagnostics.Debug.WriteLine($"LoadLikedSongsAsync: Found {tracks?.Count()} liked songs.");

                        foreach (var item in tracks)
                        {
                            var trackInfo = item["track"];
                            if (trackInfo != null)
                            {
                                var track = new Track
                                {
                                    Name = trackInfo["name"]?.ToString(),
                                    Artist = trackInfo["artists"]?[0]?["name"]?.ToString(),
                                    SpotifyTrackId = trackInfo["id"]?.ToString()
                                };

                                _liked.Add(track);
                                System.Diagnostics.Debug.WriteLine($"LoadLikedSongsAsync: Added track: {track.Name} by {track.Artist}");
                            }
                        }

                        LikedSongsListView.ItemsSource = _liked;
                        System.Diagnostics.Debug.WriteLine("LoadLikedSongsAsync: Liked songs list view updated.");
                        _offset += _limit;
                    }
                    else
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            if (!await RefreshTokenAsync(httpClient, request))
                            {
                                await ShowPopupAsync("Failed to refresh access token.");
                            }
                            else
                            {
                                await LoadAccountDataAsync();
                            }
                        }
                        else
                        {
                            await ShowPopupAsync("Failed to load user data.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("LoadLikedSongsAsync: Exception occurred: " + ex.Message);
                    await ShowPopupAsync("An error occurred while loading liked songs.");
                }
            }

            _isLoading = false;
            System.Diagnostics.Debug.WriteLine("LoadLikedSongsAsync: Finished loading liked songs.");
        }


        private async void LikedSongsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selectedTrack = e.ClickedItem as Track;

            if (selectedTrack != null)
            {
       
                foreach (var track in _recommendations) 
                {
                    track.IsSelected = false;
                }

              
                selectedTrack.IsSelected = true;

                string trackName = selectedTrack.Name;
                string artistName = selectedTrack.Artist;
                string spotifyTrackId = selectedTrack.SpotifyTrackId;

                _currentTrackIndex = _liked.IndexOf(selectedTrack);
                _currentListType = "Liked";


                System.Diagnostics.Debug.WriteLine($"Liked Song clicked: {trackName} by {artistName}");

                await PlayTrackAsync(trackName, artistName, spotifyTrackId);
            }
        }


        private async void RecommendationsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selectedTrack = e.ClickedItem as Track;

            if (selectedTrack != null)
            {
                string trackName = selectedTrack.Name;
                string artistName = selectedTrack.Artist;
                string spotifyTrackId = selectedTrack.SpotifyTrackId;

                _currentTrackIndex = _recommendations.IndexOf(selectedTrack);
                _currentListType = "Recommendations"; 

                System.Diagnostics.Debug.WriteLine($"Rec Song clicked: {trackName} by {artistName}");

                await PlayTrackAsync(trackName, artistName, spotifyTrackId);
            }
        }

        private async Task LoadPlaylistsAsync()
        {
            if (_isLoading) return;
            _isLoading = true;

            string accessToken = Settings.AccessToken;

            using (var httpClient = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.spotify.com/v1/me/playlists?offset={_offset}&limit={_limit}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine("Playlists JSON Response: " + jsonResponse);

                    var playlistsData = JObject.Parse(jsonResponse);
                    var playlists = playlistsData["items"];

                    foreach (var item in playlists)
                    {
                        var imagesArray = item["images"] as JArray;
                        var images = new List<PlaylistImage>();

                        if (imagesArray != null)
                        {
                            foreach (var image in imagesArray)
                            {
                                images.Add(new PlaylistImage
                                {
                                    Url = image["url"].ToString()
                                });
                            }
                        }

                        var playlist = new Playlist
                        {
                            Id = item["id"].ToString(),
                            Name = item["name"].ToString(),
                            Images = images
                        };

                        _playlists.Add(playlist);
                    }

                    _offset += _limit;
                }
                else
                {
                    await ShowPopupAsync("Failed to load playlists.");
                }
            }

            _isLoading = false;
        }

        private async Task LoadTracksForPlaylistAsync(Playlist playlist)
        {
            string accessToken = Settings.AccessToken;

            using (var httpClient = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.spotify.com/v1/playlists/{playlist.Id}/tracks");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine("Raw JSON Response: " + jsonResponse); 

                    var tracksData = JObject.Parse(jsonResponse)["items"];
                    if (tracksData != null && tracksData.HasValues)
                    {
                        playlist.Tracks.Clear();

                        foreach (var item in tracksData)
                        {
                            var trackInfo = item["track"];
                            if (trackInfo != null)
                            {
                                var track = new Track
                                {
                                    Name = trackInfo["name"]?.ToString(), 
                                    Artist = trackInfo["artists"]?[0]?["name"]?.ToString(),
                                    SpotifyTrackId = trackInfo["id"]?.ToString()

                                };

                                playlist.Tracks.Add(track);
                            }
                            else
                            {
                                Debug.WriteLine("Track info is null for item: " + item);
                            }
                        }

                        TracksListView.ItemsSource = playlist.Tracks; 
                    }
                    else
                    {
                        Debug.WriteLine("No tracks found in response.");
                        await ShowPopupAsync("No tracks found in the playlist.");
                    }
                }
                else
                {
                    await ShowPopupAsync("Failed to load tracks: " + response.ReasonPhrase);
                }
            }
        }

        private void PlaylistsListView_ScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null && scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight)
            {
                LoadPlaylistsAsync();
            }
        }

        private void LikedSongsListView_ScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null && scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight)
            {
                LoadLikedSongsAsync();
            }
        }

        private async void PlaylistsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selectedPlaylist = e.ClickedItem as Playlist;

            if (selectedPlaylist != null)
            {
                await LoadTracksForPlaylistAsync(selectedPlaylist);
                PlaylistsListView.Visibility = Visibility.Collapsed;
                TracksListView.Visibility = Visibility.Visible;
                BackButtonTrack.Visibility = Visibility.Visible;
            }
        }


        private async void TrackItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var stackPanel = sender as StackPanel;
            if (stackPanel != null)
            {
                var track = stackPanel.DataContext as Track;
                if (track != null)
                {
                    string trackName = track.Name;
                    string artistName = track.Artist;
                    string spotifyTrackId = track.SpotifyTrackId;

                    System.Diagnostics.Debug.WriteLine($"Track clicked: {trackName} by {artistName}");

                    _currentTrackIndex = _currentPlaylist.IndexOf(track);
                    _currentListType = "PlaylistTracks"; 

                    await PlayTrackAsync(trackName, artistName, spotifyTrackId);
                }
            }
        }

        private void BackButtonTrack_Click(object sender, RoutedEventArgs e)
        {

            TracksListView.Visibility = Visibility.Collapsed;
            PlaylistsListView.Visibility = Visibility.Visible;
            BackButtonTrack.Visibility = Visibility.Collapsed;
        }

        private void SpotifyWebView_NavigationFailed(WebView sender, WebViewNavigationFailedEventArgs args)
        {
            Debug.WriteLine($"Navigation failed: {args.Uri}, Error: {args.WebErrorStatus}");
        }

        private async Task LoadAccountDataAsync()
        {
            string accessToken = Settings.AccessToken;

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    var response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine("User Data JSON Response: " + jsonResponse);

                        var userData = JObject.Parse(jsonResponse);

                        string userName = userData["display_name"]?.ToString() ?? "unknown user";
                        string userID = userData["id"]?.ToString() ?? "no id";
                        string userCountry = userData["country"]?.ToString() ?? "no country";

                        UserNameTextBlock.Text = $"Display Name: {userName}";
                        UserIDTextBlock.Text = $"User ID: {userID}";
                        UserCountryTextBlock.Text = $"Country: {userCountry}";

                        var imagesArray = userData["images"] as JArray;
                        if (imagesArray != null && imagesArray.Count > 0)
                        {
                            string profileImageUrl = imagesArray[0]["url"]?.ToString();

                            if (!string.IsNullOrEmpty(profileImageUrl))
                            {
                                ProfileImage.Source = new BitmapImage(new Uri(profileImageUrl));
                            }
                        }
                        else
                        {
                            ProfileImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/place_holder_pfp.jpg"));
                        }
                    }
                    else
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            if (!await RefreshTokenAsync(httpClient, request))
                            {
                                await ShowPopupAsync("Failed to refresh access token.");
                            }
                            else
                            {
                                await LoadAccountDataAsync();
                            }
                        }
                        else
                        {
                            await ShowPopupAsync("Failed to load user data.");
                        }
                    }
                }
                catch (InvalidOperationException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"InvalidOperationException occurred: {ex.Message}");
                    await ShowPopupAsync("An error occurred while loading account data. Please try again.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"An unexpected error occurred: {ex.Message}");
                    await ShowPopupAsync("An unexpected error occurred. Please try again.");
                }
            }
        }


        private async Task<string> SearchTrackAsync(string trackName, string artistName)
        {
            string innerTubeUrl = $"https://www.youtube.com/youtubei/v1/search?key={Settings.InnerTubeAPIKey}";

            using (var httpClient = new HttpClient())
            {
                var query = $"{trackName} by {artistName} category: music";

                var searchData = new SearchData
                {
                    Query = query,
                    Context = new Context
                    {
                        Client = new Client
                        {
                            Hl = "en",
                            Gl = "US",
                            ClientName = "WEB",
                            ClientVersion = "2.20211122.09.00"
                        }
                    },
                    Params = "EgZ2A2h0AQ=="
                };

                var searchContent = new StringContent(SerializeSearchData(searchData), Encoding.UTF8, "application/json");
                var searchResponse = await httpClient.PostAsync(innerTubeUrl, searchContent);

                if (searchResponse.IsSuccessStatusCode)
                {
                    var searchJson = await searchResponse.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Search JSON Response: {searchJson}");

                    return ExtractVideoIdWithFallback(searchJson);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to search for track: {searchResponse.StatusCode}");
                    return null;
                }
            }
        }

        private string SerializeSearchData(SearchData searchData)
        {
            var jsonObject = new JsonObject
            {
                ["query"] = JsonValue.CreateStringValue(searchData.Query),
                ["context"] = new JsonObject
                {
                    ["client"] = new JsonObject
                    {
                        ["hl"] = JsonValue.CreateStringValue(searchData.Context.Client.Hl),
                        ["gl"] = JsonValue.CreateStringValue(searchData.Context.Client.Gl),
                        ["clientName"] = JsonValue.CreateStringValue(searchData.Context.Client.ClientName),
                        ["clientVersion"] = JsonValue.CreateStringValue(searchData.Context.Client.ClientVersion)
                    }
                },
                ["params"] = JsonValue.CreateStringValue(searchData.Params)
            };

            return jsonObject.Stringify();
        }

        private string ExtractVideoIdUsingJson(string json)
        {
            var jsonObject = JsonObject.Parse(json);

            if (jsonObject.ContainsKey("contents"))
            {
                var contents = jsonObject["contents"].GetObject();
                if (contents.ContainsKey("twoColumnSearchResultsRenderer"))
                {
                    var twoColumnSearchResultsRenderer = contents["twoColumnSearchResultsRenderer"].GetObject();
                    if (twoColumnSearchResultsRenderer.ContainsKey("primaryContents"))
                    {
                        var primaryContents = twoColumnSearchResultsRenderer["primaryContents"].GetObject();
                        if (primaryContents.ContainsKey("sectionListRenderer"))
                        {
                            var sectionListRenderer = primaryContents["sectionListRenderer"].GetObject();
                            if (sectionListRenderer.ContainsKey("contents"))
                            {
                                var contentsArray = sectionListRenderer["contents"].GetArray();
                                foreach (var item in contentsArray)
                                {
                                    var itemSectionRenderer = item.GetObject();
                                    if (itemSectionRenderer.ContainsKey("itemSectionRenderer"))
                                    {
                                        var itemContents = itemSectionRenderer["itemSectionRenderer"].GetObject();
                                        if (itemContents.ContainsKey("contents"))
                                        {
                                            var videoContentsArray = itemContents["contents"].GetArray();
                                            foreach (var videoItem in videoContentsArray)
                                            {
                                                var videoRenderer = videoItem.GetObject().GetNamedObject("videoRenderer");
                                                if (videoRenderer != null && videoRenderer.ContainsKey("videoId"))
                                                {
                                                    return videoRenderer["videoId"].GetString();
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

            System.Diagnostics.Debug.WriteLine("Video ID not found in the JSON response.");
            return null;
        }

        private string ExtractVideoIdWithFallback(string json)
        {
            var jsonObject = JsonObject.Parse(json);
            string musicVideoId = null;

            if (jsonObject.ContainsKey("contents"))
            {
                var contents = jsonObject["contents"].GetObject();

                if (contents.ContainsKey("twoColumnSearchResultsRenderer"))
                {
                    var twoColumnSearchResultsRenderer = contents["twoColumnSearchResultsRenderer"].GetObject();

                    if (twoColumnSearchResultsRenderer.ContainsKey("primaryContents"))
                    {
                        var primaryContents = twoColumnSearchResultsRenderer["primaryContents"].GetObject();

                        if (primaryContents.ContainsKey("sectionListRenderer"))
                        {
                            var sectionListRenderer = primaryContents["sectionListRenderer"].GetObject();

                            if (sectionListRenderer.ContainsKey("contents"))
                            {
                                var contentsArray = sectionListRenderer["contents"].GetArray();
                                foreach (var item in contentsArray)
                                {
                                    var itemSectionRenderer = item.GetObject();
                                    if (itemSectionRenderer.ContainsKey("itemSectionRenderer"))
                                    {
                                        var itemContents = itemSectionRenderer["itemSectionRenderer"].GetObject();
                                        if (itemContents.ContainsKey("contents"))
                                        {
                                            var videoContentsArray = itemContents["contents"].GetArray();
                                            foreach (var videoItem in videoContentsArray)
                                            {
                                                var videoRenderer = videoItem.GetObject().GetNamedObject("videoRenderer");
                                                if (videoRenderer != null && videoRenderer.ContainsKey("videoId"))
                                                {
                                                    var videoId = videoRenderer["videoId"].GetString();

                                                    var titleRuns = videoRenderer["title"].GetObject()["runs"].GetArray();
                                                    if (titleRuns.Count > 0)
                                                    {
                                                        var title = titleRuns[0].GetObject()["text"].GetString();

                                                        if (title.IndexOf("Music Video", StringComparison.OrdinalIgnoreCase) == -1)
                                                        {
                                                            return videoId;
                                                        }
                                                        else
                                                        {

                                                            musicVideoId = videoId;
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
                }
            }

            if (musicVideoId != null)
            {
                System.Diagnostics.Debug.WriteLine("Using music video as a fallback.");
                return musicVideoId;
            }

            System.Diagnostics.Debug.WriteLine("No video ID found in the JSON response.");
            return null;
        }

        private async Task<string> FetchAudioUrlAsync(string videoId)
        {
            try
            {
                string playerUrl = "https://www.youtube.com/youtubei/v1/player?key=" + Settings.InnerTubeAPIKey;

                using (var httpClient = new HttpClient())
                {

                    var playerData = new JsonObject
                    {
                        ["videoId"] = JsonValue.CreateStringValue(videoId),
                        ["context"] = new JsonObject
                        {
                            ["client"] = new JsonObject
                            {
                                ["hl"] = JsonValue.CreateStringValue("en"),
                                ["gl"] = JsonValue.CreateStringValue("US"),
                                ["clientName"] = JsonValue.CreateStringValue("IOS"),
                                ["clientVersion"] = JsonValue.CreateStringValue("19.29.1"),
                                ["deviceMake"] = JsonValue.CreateStringValue("Apple"),
                                ["deviceModel"] = JsonValue.CreateStringValue("iPhone"),
                                ["osName"] = JsonValue.CreateStringValue("iOS"),
                                ["osVersion"] = JsonValue.CreateStringValue("17.5.1.21F90"),
                                ["userAgent"] = JsonValue.CreateStringValue("com.google.ios.youtube/19.29.1 (iPhone16,2; U; CPU iOS 17_5_1 like Mac OS X;)")
                            }
                        }
                    };

                    var playerContent = new StringContent(playerData.Stringify(), Encoding.UTF8, "application/json");
                    var playerResponse = await httpClient.PostAsync(playerUrl, playerContent);

                    if (playerResponse.IsSuccessStatusCode)
                    {
                        var playerJson = await playerResponse.Content.ReadAsStringAsync();
                        return ExtractAudioUrl(playerJson);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to fetch audio URL: {playerResponse.StatusCode}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"An error occurred while fetching audio URL: {ex.Message}");
                return null;
            }
        }

        private async Task PlayTrackAsync(string trackName, string artistName, string spotifyTrackId)
        {
            try
            {
                var videoId = await SearchTrackAsync(trackName, artistName);

                if (videoId != null)
                {
                    var audioUrl = await FetchAudioUrlAsync(videoId);

                    if (audioUrl != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Playing audio from URL: {audioUrl}");

                        System.Diagnostics.Debug.WriteLine($"Playing spotify id: {spotifyTrackId}");

                        System.Diagnostics.Debug.WriteLine($"Video id: {videoId}");

                        TrackNameTextBlock.Text = trackName;
                        ArtistNameTextBlock.Text = artistName;

                        AudioPlayerOverlay.Visibility = Visibility.Visible;
                        AudioPlayer.Source = new Uri(audioUrl, UriKind.Absolute);
                        AudioPlayer.Visibility = Visibility.Visible;

                        PlayButton.Content = "Play";

                        currentSongID = spotifyTrackId;

                        System.Diagnostics.Debug.WriteLine($"Current Spotify ID: {currentSongID}");

                        AudioPlayer.Play();

                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Audio URL could not be fetched.");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Track not found.");
                }
            }
            catch (TypeAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine($"TypeAccessException occurred: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"An error occurred: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private string ExtractAudioUrl(string json)
        {

            System.Diagnostics.Debug.WriteLine("Received JSON: " + json);

            var jsonObject = JsonObject.Parse(json);

            if (jsonObject.ContainsKey("streamingData"))
            {
                var streamingData = jsonObject["streamingData"].GetObject();

                if (streamingData.ContainsKey("adaptiveFormats"))
                {
                    var adaptiveFormatsArray = streamingData["adaptiveFormats"].GetArray();
                    foreach (var format in adaptiveFormatsArray)
                    {
                        var formatObject = format.GetObject();

                        if (formatObject.ContainsKey("itag") &&
                            (int)formatObject["itag"].GetNumber() == 140 &&
                            formatObject.ContainsKey("url"))
                        {
                            return formatObject["url"].GetString();
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("Audio URL not found for itag 140 in the JSON response.");
            return null;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
            {
                AudioPlayer.Pause();
                StartBackgroundAudio();
                PlayButton.Content = "Play";
            }
            else
            {
                AudioPlayer.Play();
                StopBackgroundAudio();
                PlayButton.Content = "Pause";
            }
            isPlaying = !isPlaying;
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {

             PlayPreviousTrack();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {

             PlayNextTrack();
        }

        private async void LikeButton_Click(object sender, RoutedEventArgs e)
        {
            await LikeSongAsync(currentSongID);
        }

        private void AudioPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            isPlaying = false;
            PlayButton.Content = "Play";
            SeekSlider.Value = 0;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {

            AudioPlayer.Stop();

            AudioPlayer.Visibility = Visibility.Collapsed;
            SeekSlider.Value = 0;
        }

        private async void PlayNextTrack()
        {

            if (_currentListType == "PlaylistTracks" && _currentTrackIndex < _currentPlaylist.Count - 1)
            {
                _currentTrackIndex++;
                var nextTrack = _currentPlaylist[_currentTrackIndex];
                await PlayTrackAsync(nextTrack.Name, nextTrack.Artist, nextTrack.SpotifyTrackId);
                System.Diagnostics.Debug.WriteLine($"Playing next track in Playlist: {nextTrack.Name} by {nextTrack.Artist}");
            }

            else if (_currentListType == "History" && _currentTrackIndex < _recentlyPlayed.Count - 1)
            {
                _currentTrackIndex++;
                var nextTrack = _recentlyPlayed[_currentTrackIndex];
                await PlayTrackAsync(nextTrack.Name, nextTrack.Artist, nextTrack.SpotifyTrackId);
                System.Diagnostics.Debug.WriteLine($"Playing next track in History: {nextTrack.Name} by {nextTrack.Artist}");
            }
            else if (_currentListType == "Liked" && _currentTrackIndex < _liked.Count - 1)
            {
                _currentTrackIndex++;
                var nextTrack = _liked[_currentTrackIndex];
                await PlayTrackAsync(nextTrack.Name, nextTrack.Artist, nextTrack.SpotifyTrackId);
                System.Diagnostics.Debug.WriteLine($"Playing next track in Liked: {nextTrack.Name} by {nextTrack.Artist}");
            }
            else if (_currentListType == "Recommendations" && _currentTrackIndex < _recommendations.Count - 1)
            {
                _currentTrackIndex++;
                var nextTrack = _recommendations[_currentTrackIndex];
                await PlayTrackAsync(nextTrack.Name, nextTrack.Artist, nextTrack.SpotifyTrackId);
                System.Diagnostics.Debug.WriteLine($"Playing next track in Recommendations: {nextTrack.Name} by {nextTrack.Artist}");
            }
        }

        private async void PlayPreviousTrack()
        {

            if (_currentListType == "PlaylistTracks" && _currentTrackIndex > 0)
            {
                _currentTrackIndex--;
                var previousTrack = _currentPlaylist[_currentTrackIndex];
                await PlayTrackAsync(previousTrack.Name, previousTrack.Artist, previousTrack.SpotifyTrackId);
                System.Diagnostics.Debug.WriteLine($"Playing previous track in Playlist: {previousTrack.Name} by {previousTrack.Artist}");
            }

            else if (_currentListType == "History" && _currentTrackIndex > 0)
            {
                _currentTrackIndex--;
                var previousTrack = _recentlyPlayed[_currentTrackIndex];
                await PlayTrackAsync(previousTrack.Name, previousTrack.Artist, previousTrack.SpotifyTrackId);
                System.Diagnostics.Debug.WriteLine($"Playing previous track in History: {previousTrack.Name} by {previousTrack.Artist}");
            }
            else if (_currentListType == "Liked" && _currentTrackIndex > 0)
            {
                _currentTrackIndex--;
                var previousTrack = _liked[_currentTrackIndex];
                await PlayTrackAsync(previousTrack.Name, previousTrack.Artist, previousTrack.SpotifyTrackId);
                System.Diagnostics.Debug.WriteLine($"Playing previous track in Liked: {previousTrack.Name} by {previousTrack.Artist}");
            }
            else if (_currentListType == "Recommendations" && _currentTrackIndex > 0)
            {
                _currentTrackIndex--;
                var previousTrack = _recommendations[_currentTrackIndex];
                await PlayTrackAsync(previousTrack.Name, previousTrack.Artist, previousTrack.SpotifyTrackId);
                System.Diagnostics.Debug.WriteLine($"Playing previous track in Recommendations: {previousTrack.Name} by {previousTrack.Artist}");
            }
        }


        private void AudioPlayer_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            if (AudioPlayer.CurrentState == MediaElementState.Playing)
            {
                UpdateSeekSlider();
            }
        }

        private async void UpdateSeekSlider()
        {
            while (isPlaying)
            {
                SeekSlider.Maximum = AudioPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                SeekSlider.Value = AudioPlayer.Position.TotalSeconds;
                await Task.Delay(1000);
            }
        }

        private void SeekSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (AudioPlayer.NaturalDuration.HasTimeSpan && SeekSlider.Value >= 0)
            {
                AudioPlayer.Position = TimeSpan.FromSeconds(SeekSlider.Value);
            }
        }

        private void StartBackgroundAudio()
        {
            var message = new ValueSet { { "Command", "Play" } };
            BackgroundMediaPlayer.SendMessageToBackground(message);
        }

        private void StopBackgroundAudio()
        {
            var message = new ValueSet { { "Command", "Stop" } };
            BackgroundMediaPlayer.SendMessageToBackground(message);
        }

        private string ExtractVideoId(string json)
        {
            var jsonObject = JObject.Parse(json);
            var videoId = jsonObject["videoId"]?.ToString();

            if (!string.IsNullOrEmpty(videoId))
            {
                return videoId.Trim();
            }

            System.Diagnostics.Debug.WriteLine("No video ID found.");
            return null;
        }

        private async Task<bool> RefreshTokenAsync(HttpClient httpClient, HttpRequestMessage request)
        {
            var tokenManager = new TokenManager();
            var newAccessToken = await tokenManager.RefreshAccessTokenAsync();

            if (newAccessToken != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
                var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine("User Data JSON Response (after refresh): " + jsonResponse);

                    try
                    {

                        var jsonObject = JsonObject.Parse(jsonResponse);

                        return true;
                    }
                    catch (JsonException jsonEx)
                    {
                        System.Diagnostics.Debug.WriteLine("JSON parsing error: " + jsonEx.Message);

                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("An unexpected error occurred: " + ex.Message);

                    }
                }
            }

            return false;
        }

        private async Task ShowPopupAsync(string message)
        {
            var dialog = new MessageDialog(message);
            await dialog.ShowAsync();
        }
    }
}

public class Playlist
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<PlaylistImage> Images { get; set; }
    public List<Track> Tracks { get; set; } = new List<Track>(); 
}

public class Track
{
    public string Name { get; set; }
    public string Artist { get; set; }
    public string SpotifyTrackId { get; set; }
    public bool IsSelected { get; set; }
}


public class PlaylistImage
{
    public string Url { get; set; }
}

public class PlaylistsResponse
{
    public List<Playlist> Items { get; set; }
}

public class SearchData
{
    public string Query { get; set; }
    public Context Context { get; set; }
    public string Params { get; set; }
}

public class Context
{
    public Client Client { get; set; }
}

public class Client
{
    public string Hl { get; set; }
    public string Gl { get; set; }
    public string ClientName { get; set; }
    public string ClientVersion { get; set; }
}