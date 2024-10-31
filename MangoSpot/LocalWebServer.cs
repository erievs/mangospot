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
    }

    private async void OnConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
    {
        using (var reader = new StreamReader(args.Socket.InputStream.AsStreamForRead(), Encoding.UTF8))
        using (var writer = new StreamWriter(args.Socket.OutputStream.AsStreamForWrite(), Encoding.UTF8))
        {
            string requestLine = await reader.ReadLineAsync();
            string method = requestLine.Split(' ')[0];
            string path = requestLine.Split(' ')[1];

            string localIPAddress = GetLocalIPAddress();

            if (method == "GET" && path.StartsWith("/api/submit"))
            {
                string queryString = path.Substring(path.IndexOf('?') + 1);
                var parameters = ParseFormData(queryString);

                LogParameters(parameters);

                if (parameters.Count > 0)
                {
                    string apiResponseString = $@"
                <html>
                <body>
                    <h1>API Submission Successful!</h1>
                    <p>Client Secret: {parameters.GetValueOrDefault("clientSecret", string.Empty)}</p>
                    <p>Client ID: {parameters.GetValueOrDefault("clientID", string.Empty)}</p>
                    <p>Redirect URL: {parameters.GetValueOrDefault("redirectUri", string.Empty)}</p>
                    <p>Callback Code: {parameters.GetValueOrDefault("code", string.Empty)}</p>
                </body>
                </html>";

                    ClientSecret = parameters.GetValueOrDefault("clientSecret", string.Empty);
                    ClientID = parameters.GetValueOrDefault("clientID", string.Empty);
                    RedirectUri = parameters.GetValueOrDefault("redirectUri", string.Empty);
                    Code = parameters.GetValueOrDefault("code", string.Empty);

                    ParametersSet?.Invoke();

                    writer.WriteLine("HTTP/1.1 200 OK");
                    writer.WriteLine("Content-Type: text/html");
                    writer.WriteLine($"Content-Length: {apiResponseString.Length}");
                    writer.WriteLine();
                    await writer.WriteLineAsync(apiResponseString);
                    await writer.FlushAsync();
                }
                else
                {
                    string errorResponseString = $@"
                <html>
                <body>
                    <h1>Error: No parameters provided!</h1>
                    <p>Please provide at least one parameter to submit.</p>
                    <a href='/' >Go back</a>
                </body>
                </html>";

                    writer.WriteLine("HTTP/1.1 400 Bad Request");
                    writer.WriteLine("Content-Type: text/html");
                    writer.WriteLine($"Content-Length: {errorResponseString.Length}");
                    writer.WriteLine();
                    await writer.WriteLineAsync(errorResponseString);
                    await writer.FlushAsync();
                }
            }
            else
            {
                string formAction = $"/api/submit";
                string responseString = $@"
            <html>
            <body>
                <h1>Input Your Details</h1>
                <form method='GET' action='{formAction}'>
                    <label for='clientSecret'>Client Secret:</label><br>
                    <input type='text' id='clientSecret' name='clientSecret'><br><br>
                    <label for='clientID'>Client ID:</label><br>
                    <input type='text' id='clientID' name='clientID'><br><br>
                    <label for='redirectUri'>Redirect URL:</label><br>
                    <input type='text' id='redirectUri' name='redirectUri'><br><br>
                    <label for='code'>Callback Code:</label><br>
                    <input type='text' id='code' name='code'><br><br>
                    <input type='submit' value='Submit'>
                </form>
            </body>
            </html>";

                writer.WriteLine("HTTP/1.1 200 OK");
                writer.WriteLine("Content-Type: text/html");
                writer.WriteLine($"Content-Length: {responseString.Length}");
                writer.WriteLine();
                await writer.WriteLineAsync(responseString);
                await writer.FlushAsync();
            }
        }
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