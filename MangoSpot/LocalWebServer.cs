using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Networking.Connectivity;
using MangoSpot;

public class LocalWebServer
{
    private StreamSocketListener listener;

    public string ClientSecret { get; private set; }
    public string ClientID { get; private set; }
    public string RedirectUri { get; private set; }
    public string Code { get; private set; }
    public static bool IsRunning;

    public event Action ParametersSet;

    public LocalWebServer(int port)
    {
        listener = new StreamSocketListener();
        listener.ConnectionReceived += OnConnectionReceived;
    }

    public async Task Start(int port)
    {
        await listener.BindServiceNameAsync(port.ToString());
        string localIPAddress = GetLocalIPAddress();
        Debug.WriteLine($"Server running at http://{localIPAddress}:{port}/");
        IsRunning = true;
    }

    private async void OnConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
    {
        try
        {
            using (var reader = new StreamReader(args.Socket.InputStream.AsStreamForRead(), Encoding.UTF8))
            using (var writer = new StreamWriter(args.Socket.OutputStream.AsStreamForWrite(), Encoding.UTF8))
            {
                string requestLine = await reader.ReadLineAsync();
                string method = requestLine.Split(' ')[0];
                string path = requestLine.Split(' ')[1];

                if (method == "GET" && path.StartsWith("/api/submit"))
                {
                    string queryString = path.Substring(path.IndexOf('?') + 1);
                    var parameters = ParseFormData(queryString);

                    LogParameters(parameters);

                    if (parameters.Count > 0)
                    {
                        string apiResponseString = GenerateSuccessResponse(parameters);

                        // Set parameters and invoke event
                        ClientSecret = parameters.GetValueOrDefault("clientSecret", string.Empty);
                        ClientID = parameters.GetValueOrDefault("clientID", string.Empty);
                        RedirectUri = parameters.GetValueOrDefault("redirectUri", string.Empty);
                        Code = parameters.GetValueOrDefault("code", string.Empty);

                        ParametersSet?.Invoke();

                        await SendResponse(writer, "200 OK", apiResponseString);
                    }
                    else
                    {
                        string errorResponseString = GenerateErrorResponse("Error: No parameters provided!", "Please provide at least one parameter to submit.");
                        await SendResponse(writer, "400 Bad Request", errorResponseString);
                    }
                }
                else
                {
                    string formAction = $"/api/submit";
                    string responseString = GenerateFormResponse(formAction);
                    await SendResponse(writer, "200 OK", responseString);
                }
            }
        }
        catch (Exception ex)
        {
            string errorResponseString = GenerateErrorResponse("Error: Unexpected issue occurred!", "Please try again later.");
            Debug.WriteLine(errorResponseString);
        }
    }

    private async Task SendResponse(StreamWriter writer, string statusCode, string responseString)
    {
        writer.WriteLine($"HTTP/1.1 {statusCode}");
        writer.WriteLine("Content-Type: text/html");
        writer.WriteLine($"Content-Length: {responseString.Length}");
        writer.WriteLine();
        await writer.WriteLineAsync(responseString);
        await writer.FlushAsync();
    }

    private string GenerateSuccessResponse(Dictionary<string, string> parameters)
    {
        return $@"
        <html>
        <head>
            <title>Submission Successful</title>
            <link href='https://fonts.googleapis.com/css?family=Segoe+UI' rel='stylesheet' />
            <link href='https://cdnjs.cloudflare.com/ajax/libs/metro/4.4.0/css/metro-all.min.css' rel='stylesheet'>
            <style>
            body {{
                    display: flex; 
                    justify-content: center; 
                    align-items: center; 
                    height: 100vh; 
                    margin: 0; 
                    background-color: #2D2D30; 
                    color: #FFFFFF; 
                    font-family: 'Segoe UI', sans-serif; 
                }}
                .container {{
                    max-width: 400px; 
                    padding: 20px; 
                    background: #1A1A1A; 
                    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.5); 
                    text-align: center; 
                }}
                h1 {{
                    color: #0078D7; 
                    font-size: 24px; 
                    margin-bottom: 20px; 
                }}
                p {{
                    font-size: 16px; 
                    margin: 10px 0; 
                }}
                a {{
                    color: #0078D7; 
                    text-decoration: none; 
                }}
                a:hover {{
                    text-decoration: underline; 
                }}
            </style>
        </head>
        <body>
            <div class='container'>
                <h1>Details Sent To App Successfully!</h1>
                <p><strong>Client Secret:</strong> {parameters.GetValueOrDefault("clientSecret", string.Empty)}</p>
                <p><strong>Client ID:</strong> {parameters.GetValueOrDefault("clientID", string.Empty)}</p>
                <p><strong>Redirect URL:</strong> {parameters.GetValueOrDefault("redirectUri", string.Empty)}</p>
                <p><strong>Callback Code:</strong> {parameters.GetValueOrDefault("code", string.Empty)}</p>
                <div>
                    <a href='/'>Go back to the home page</a>
                </div>
            </div>
        </body>
        </html>";
    }


    private string GenerateErrorResponse(string title, string message)
    {
        return $@"
        <html>
        <head>
            <title>Error</title>
            <link href='https://fonts.googleapis.com/css?family=Segoe+UI' rel='stylesheet' />
            <link href='https://cdnjs.cloudflare.com/ajax/libs/metro/4.4.0/css/metro-all.min.css' rel='stylesheet'>
            <style>
                body {{
                    display: flex; 
                    justify-content: center; 
                    align-items: center; 
                    height: 100vh; 
                    margin: 0; 
                    background-color: #2D2D30; 
                    color: #FFFFFF; 
                    font-family: 'Segoe UI', sans-serif; 
                }}
                .container {{
                    max-width: 400px; 
                    padding: 20px; 
                    background: #1A1A1A; 
                    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.5); 
                    text-align: center; 
                }}
                h1 {{
                    color: #0078D7; 
                    font-size: 24px; 
                    margin-bottom: 20px; 
                }}
                p {{
                    font-size: 16px; 
                    margin: 10px 0; 
                    text-overflow: ellipsis;
                }}
                a {{
                    color: #0078D7; 
                    text-decoration: none; 
                }}
                a:hover {{
                    text-decoration: underline; 
                }}
            </style>
        </head>
        <body>
            <div class='container'>
                <h1>{title}</h1>
                <p>{message}</p>
                <a href='/'>Go back</a>
            </div>
        </body>
        </html>";
    }

    private string GenerateFormResponse(string formAction)
    {
        return $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Input Your Details</title>
            <link href='https://fonts.googleapis.com/css?family=Segoe+UI' rel='stylesheet'>
            <link href='https://cdnjs.cloudflare.com/ajax/libs/metro/4.4.0/css/metro-all.min.css' rel='stylesheet'>
            <style>
                body {{
                    display: flex; 
                    justify-content: center; 
                    align-items: center; 
                    height: 100vh; 
                    margin: 0; 
                    background-color: #2D2D30; 
                    color: #FFFFFF; 
                    font-family: 'Segoe UI', sans-serif; 
                }}
                .container {{
                    max-width: 800px; 
                    margin: auto; 
                    padding: 20px; 
                    background: #1A1A1A; 
                    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.5); 
                }}
                h1 {{
                    text-align: center; 
                    color: #0078D7; 
                    font-size: 24px; 
                    margin-bottom: 20px; 
                }}
                input[type='text'] {{
                    width: 100%; 
                    padding: 10px; 
                    margin: 10px 0; 
                    border: none; 
                    background-color: #3E3E42; 
                    color: #FFFFFF; 
                    font-size: 16px; 
                }}
                input[type='submit'] {{
                    background-color: #0078D7; 
                    color: white; 
                    border: none; 
                    cursor: pointer; 
                    padding: 10px; 
                    width: 100%; 
                    margin-top: 15px; 
                    font-size: 16px; 
                    transition: background-color 0.3s; 
                    height: 46px;
                }}
                input[type='submit']:hover {{
                    background-color: #005A9E; 
                }}
                p {{
                text-overflow: ellipsis;
                }}

            </style>
        </head>
        <body>
            <div class='container'>
                <h1>Input MangoSpot Details</h1>
                <form method='GET' action='{formAction}'>
                   <label for='clientSecret'>Client Secret:</label>
                    <input type='text' id='clientSecret' name='clientSecret' required>
                    <div class='explanation'>The Client Secret is a confidential string used for authentication. It should be kept secure and not shared publicly.</div>
                     <br>
                    <label for='clientID'>Client ID:</label>
                    <input type='text' id='clientID' name='clientID' required>
                    <div class='explanation'>The Client ID is a public identifier for your application. It is used to identify your app to Spotify.</div>

                   <br>
                    <label for='redirectUri'>Redirect URL:</label>
                    <input type='text' id='redirectUri' name='redirectUri' required>
                    <div class='explanation'>The Redirect URL is the endpoint where users will be redirected after authentication. It must match the URL registered with Spotify.</div>
                    <br>

                    <label for='code'>Callback Code:</label>
                    <input type='text' id='code' name='code' required>
                    <br>
                    <div class='explanation'>The Callback Code is a temporary code returned after the user authorizes access. It is used to obtain an access token for Spotify.</div>

                    <input type='submit' value='Submit'>
                </form>
            </div>
            
            <script src='https://cdnjs.cloudflare.com/ajax/libs/jquery/3.6.0/jquery.min.js'></script>
            <script src='https://cdnjs.cloudflare.com/ajax/libs/metro/4.4.0/js/metro.min.js'></script>
        </body>
        </html>";
    }

    private void LogParameters(Dictionary<string, string> parameters)
    {
        Debug.WriteLine("Form parameters received:");
        foreach (var parameter in parameters)
        {
            Debug.WriteLine(string.Format("{0}: {1}", parameter.Key, parameter.Value));
        }

        var expectedParameters = new[] { "clientSecret", "clientID", "redirectUri", "code" };
        foreach (var expected in expectedParameters)
        {
            string value;
            if (parameters.TryGetValue(expected, out value))
            {
                Debug.WriteLine(string.Format("{0}: {1}", expected, value));

                switch (expected)
                {
                    case "clientSecret":
                        Settings.ClientSecret = value;
                        break;
                    case "clientID":
                        Settings.ClientID = value;
                        break;
                    case "redirectUri":
                        Settings.RedirectUri = value;
                        break;
                    case "code":

                        string extractedCode = ExtractCode(value);
                        Settings.AccessToken = extractedCode;

                        if (!string.IsNullOrEmpty(extractedCode))
                        {
                            Debug.WriteLine("Extracted Authorization Code: " + extractedCode);

                        }
                        else
                        {
                            Debug.WriteLine("Authorization code not found in the provided value.");
                        }
                        break;
                }
            }
            else
            {
                Debug.WriteLine(string.Format("{0}: Not provided", expected));
            }
        }
    }

    private string ExtractCode(string content)
    {
        string code = string.Empty;
        if (content.Contains("callback?code="))
        {
            code = content.Split(new[] { "callback?code=" }, StringSplitOptions.None)[1].Split('&')[0];
            Debug.WriteLine($"Extracted Code: {code}");
        }
        return code;
    }

    private Dictionary<string, string> ParseFormData(string body)
    {
        var parameters = new Dictionary<string, string>();
        string[] pairs = body.Split('&');

        foreach (string pair in pairs)
        {
            string[] keyValue = pair.Split('=');
            if (keyValue.Length == 2)
            {
                string key = Uri.UnescapeDataString(keyValue[0]);
                string value = Uri.UnescapeDataString(keyValue[1]);
                parameters[key] = value;
            }
        }
        return parameters;
    }

    public void Stop()
    {
        listener.Dispose();
    }

    private string GetLocalIPAddress()
    {
        var hostNames = NetworkInformation.GetHostNames();
        foreach (var hostName in hostNames)
        {
            if (hostName.IPInformation != null && hostName.Type == Windows.Networking.HostNameType.Ipv4)
            {
                return hostName.CanonicalName;
            }
        }
        return "No valid IP Address found.";
    }
}

public static class DictionaryExtensions
{
    public static string GetValueOrDefault(this Dictionary<string, string> dictionary, string key, string defaultValue)
    {
        string value;
        if (dictionary.TryGetValue(key, out value))
        {
            return value;
        }
        return defaultValue;
    }
}