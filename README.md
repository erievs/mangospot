MangoTube

A Windows 8.1 and Windows 10 Spotify Client

Overview: MangoTube uses InnerTube ONLY for fetching song files. All other functionalities require a Spotify account. We search for music on YouTube using InnerTube by combining song title and artist, and filter results to ensure they are music-related. While there may be occasional mismatches, this method generally provides a reliable experience. Note: some features may require a Spotify Premium account.

Login Process: Due to limitations on Windows Phone 8.1 browser, the Spotify login page may not always load smoothly. Songs streamed through MangoTube will not appear in your Spotify history (this is due to Spotify API limitations). You can, however, still view your listening history within MangoTube.

Features like playlist creation and Spotify web control are under development.

    This is a Beta release.

How to Login to Spotify

To use MangoTube, you’ll need to set up an app on the Spotify Developer Dashboard. For more information, check out this guide.

    Note: Beta 1.0.0 requires your redirect URL to be http://localhost:3000/callback.

Steps to Get Started

    Create an app on Spotify Developer Dashboard.
    Save your Redirect URL, Client Secret, and Client ID.

Methods to Input Spotify Details into MangoTube
Method 1: Manual Entry

    Least recommended due to complexity. Manually enter the Client ID, Client Secret, and Redirect URL in the app.

Method 2: CSV Import

    Prepare a CSV file with three fields: Client ID, Client Secret, Redirect URL.
    Import this file into the app.

Method 3: Web Panel

    Start the server, connect it to your device, and use the web panel for a more seamless entry experience.

Obtaining Your Spotify Auth Code

After entering your app details, generate a QR code within the app. Scan the QR code on a mobile device or use a webcam to access Spotify’s login page and authorize MangoTube.

Once authorized, copy the entire redirect URL (or at least the part after ?code=).
Methods to Input the Auth Code into MangoTube
Method 1: Manual Entry

    Paste only the code from ?code= onwards directly into the app.

Method 2: CSV Import

    Paste the redirect URL into a CSV file and import it into MangoTube.

Method 3: Web Panel

    Use the web panel to enter the full redirect URL with your Spotify auth code.

    Note: Make sure you have the complete redirect URL for a successful login.

MangoTube is currently in Beta, and we welcome feedback on user experience and additional features. Enjoy or somethin!
