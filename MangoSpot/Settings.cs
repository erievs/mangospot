﻿using Windows.Storage;

namespace MangoSpot
{
    public static class Settings
    {
        private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public static string ClientID { get; set; } = "67b353570c814eed91bd60678460cac1";
        public static string ClientSecret { get; set; } = "1ba00551ebf042cc9218ba2a02c48070";
        public static string RedirectUri { get; set; } = "http://localhost:3000/callback";

        public const string Version = "Version: Beta 1.2.5";

        private static string accessToken;

        private static bool shuffleTracks;

        private static string refreshToken;

        private static bool preventSleep;

        public static string InnerTubeAPIKey = "AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8";

        public static bool PreventSleep
        {
            get
            {
                if (localSettings.Values.ContainsKey("PreventSleep"))
                {
                    return (bool)localSettings.Values["PreventSleep"];
                }
                return preventSleep;
            }
            set
            {
                preventSleep = value;
                localSettings.Values["PreventSleep"] = value;
            }
        }

        public static string AccessToken
        {
            get
            {
                if (accessToken == null && localSettings.Values.ContainsKey("AccessToken"))
                {
                    accessToken = localSettings.Values["AccessToken"] as string;
                }
                return accessToken;
            }
            set
            {
                accessToken = value;
                localSettings.Values["AccessToken"] = value;
            }
        }

        public static bool ShuffleTracks
        {
            get
            {
                if (localSettings.Values.ContainsKey("ShuffleTracks"))
                {
                    return (bool)localSettings.Values["ShuffleTracks"];
                }
                return shuffleTracks;
            }
            set
            {
                shuffleTracks = value;
                localSettings.Values["ShuffleTracks"] = value;
            }
        }

        public static string RefreshToken
        {
            get
            {
                if (refreshToken == null && localSettings.Values.ContainsKey("RefreshToken"))
                {
                    refreshToken = localSettings.Values["RefreshToken"] as string;
                }
                return refreshToken;
            }
            set
            {
                refreshToken = value;
                localSettings.Values["RefreshToken"] = value;
            }
        }

        public static bool IsLoggedIn => !string.IsNullOrEmpty(AccessToken);

        public static void ClearAccessToken()
        {
            if (localSettings.Values.ContainsKey("AccessToken"))
            {
                localSettings.Values.Remove("AccessToken");
            }
            accessToken = null;
        }

        public static void ClearRefreshToken()
        {
            if (localSettings.Values.ContainsKey("RefreshToken"))
            {
                localSettings.Values.Remove("RefreshToken");
            }
            refreshToken = null;
        }

        public static void ClearTokens()
        {
            ClearAccessToken();
            ClearRefreshToken();
        }

    }
}
