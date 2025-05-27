using OSCVRCWiz.Services.Speech.TextToSpeech;
using OSCVRCWiz.Services.Text;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes; // Add this at the top if not present

namespace OSCVRCWiz.Services.Speech.Speech_Recognition {
    public class WebSocketServer {

        private static HttpListener httpListener;
        private static CancellationTokenSource cancellationTokenSource;
        public static string WebSocketServerPort = "9008";
        private static bool SocketServerEnabled = false;


        public static void StartServer()
        {

            cancellationTokenSource = new CancellationTokenSource();
            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://+:{WebSocketServerPort}/"); // Adjust the URL as needed
            httpListener.Start();
            SocketServerEnabled = true;
            OutputText.outputLog($"WebSocket Server Listening ({WebSocketServerPort})");

            // Run the server in a separate task
            Task.Run(() => StartListeningAsync(cancellationTokenSource.Token));

        }

        public static void StopServer()
        {

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                httpListener.Close();
                SocketServerEnabled = false;
                OutputText.outputLog("WebSocket server stopped.");
            }

        }
        public static void ToggleServer()
        {
            try
            {
                if (SocketServerEnabled == false)
                {
                    StartServer();
                } else
                {
                    StopServer();
                }
            }
            catch (System.Exception ex)
            {
                OutputText.outputLog("WebSocket Server Error: " + ex.Message, Color.Red);
            }


        }
        public static void ActivateOnStartUp()
        {
            if (VoiceWizardWindow.MainFormGlobal.rjToggleActivateWebsocketOnStart.Checked)
            {
                ToggleServer();
            }
        }


        private static async Task StartListeningAsync(CancellationToken cancellationToken)
        {

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    HttpListenerContext context = await httpListener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
                        WebSocket webSocket = webSocketContext.WebSocket;

                        await HandleWebSocketConnection(webSocket);
                    } else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
                catch (HttpListenerException ex)
                {
                    OutputText.outputLog("WebSocket: " + ex.Message, Color.Orange);
                }
            }
        }


        public static async Task HandleWebSocketConnection(WebSocket webSocket)
        {
            byte[] buffer = new byte[1024];
            WebSocketReceiveResult result;

            while (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        // Try to parse as JSON
                        try
                        {
                            JsonNode? node = JsonNode.Parse(message);
                            if (node != null)
                            {
                                TTSMessageQueue.QueueJSONMessage(message);
                            }
                        }
                        catch (Exception)
                        {
                            // Not valid JSON, optionally handle as plain text or ignore
                            OutputText.outputLog("Received non-JSON text message via WebSocket.");
                            TTSMessageQueue.QueueMessage(message, "Web App");
                        }
                    } else
                    {
                        OutputText.outputLog($"Received message: " + result.MessageType.ToString());
                    }
                }
                catch (System.Exception ex)
                {
                    OutputText.outputLog("Websocket exception: " + ex.Message);
                    try
                    {
                        OutputText.outputLog("Websocket exception inner: " + ex.InnerException.Message);
                    }
                    catch { }
                }

            }
        }
    }
}
