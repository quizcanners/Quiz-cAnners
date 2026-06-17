#nullable enable

using QuizCanners.Inspect;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace QuizCanners.Utils
{
    public static partial class QcNet
    {
        public abstract class UDP_ThreadControllerBase : IPEGI
        {
            protected UdpClient? UDPClient;
            public volatile bool portIsListening;
            private readonly Gate.UnityTimeUnScaled _sinceLastUdpRestart = new();
            protected volatile int threadVersion;
            protected volatile ThreadState threadState;

            protected enum ThreadState
            {
                NotStarted,
                Running,
                Stopped,
                Starting,
                Receiving,
                Processing,
                ExitedLoop,
                ClosingLoop,
            }

            public abstract int Port { get; }
            protected virtual AddressFamily ListenerAddressFamily => AddressFamily.InterNetworkV6;
            protected virtual IPAddress BindAddress => IPAddress.IPv6Any;
            protected virtual IPAddress ReceiveAddress => BindAddress;
            protected virtual bool UseDualMode => ListenerAddressFamily == AddressFamily.InterNetworkV6;
            protected virtual bool AllowAddressReuse => true;
            protected virtual bool AllowBroadcast => false;

            public virtual void InThread_OnStart()
            {

            }

            public abstract void InThread_ProcessIncoming(byte[] data, IPEndPoint remoteEndPoint, UdpClient client);

            private void Clear()
            {
                Interlocked.Increment(ref threadVersion);

                try
                {
                    UDPClient?.Close();
                    UDPClient?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error closing UDP client on port {Port}: {ex.Message}");
                }
                finally
                {
                    UDPClient = null;
                }
            }

            public void SetThreadDirty()
            {
                Interlocked.Increment(ref threadVersion);
            }

            public void ManagedUpdate()
            {
                if (portIsListening || threadState == ThreadState.Stopped)
                    return;

                if (!_sinceLastUdpRestart.TryConsume_IfElapsedOrFirst(1))
                    return;

                try
                {
                    Clear();

                    var port = (int)Port;

                    UDPClient = new UdpClient(ListenerAddressFamily);

                    if (ListenerAddressFamily == AddressFamily.InterNetworkV6)
                    {
                        // Allow IPv4-mapped IPv6 too, when OS supports it.
                        UDPClient.Client.DualMode = UseDualMode;
                    }

                    UDPClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, AllowAddressReuse);
                    UDPClient.EnableBroadcast = AllowBroadcast;
                    SuppressUdpConnectionReset(UDPClient.Client);

                    UDPClient.Client.Bind(new IPEndPoint(BindAddress, port));

                    UDPClient.Client.ReceiveTimeout = 1000; // 1 second

                    portIsListening = true;

                    var thread = new Thread(() => TryCatchWrapper(UDPClient, port))
                    {
                        IsBackground = true,
                        Name = $"UDP Listener {port}"
                    };

                    thread.Start();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to start UDP listener on port {Port}: {ex.Message}");
                    Debug.LogException(ex);
                    UDPClient = null;
                }
            }

            protected void TryCatchWrapper(UdpClient client, int port)
            {
                try
                {
                    threadState = ThreadState.Starting;

                    int thisThread = threadVersion;
                    InThread_OnStart();

                    while (thisThread == threadVersion)
                    {
                        try
                        {
                            IPEndPoint incomingEndPoint = new(ReceiveAddress, 0);
                            threadState = ThreadState.Receiving;
                            byte[] receivedBytes = client.Receive(ref incomingEndPoint);

                            threadState = ThreadState.Processing;

                            InThread_ProcessIncoming(receivedBytes, incomingEndPoint, client);

                        }
                        catch (SocketException ex)
                        {
                            switch (ex.SocketErrorCode)
                            {
                                case SocketError.ConnectionReset:
                                case SocketError.TimedOut:
                                    break;
                                case SocketError.Interrupted:
                                case SocketError.NotConnected:
                                case SocketError.NotSocket:
                                    return;
                                case SocketError.NetworkUnreachable:
                                case SocketError.ConnectionAborted:
                                case SocketError.AddressAlreadyInUse:
                                default:
                                    QcLog.ChillLogger.LogExceptionExpOnly(ex, key: "Listener: " + ex.SocketErrorCode.ToString());
                                    break;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Re-throw to outer handler for clean shutdown
                            throw;
                        }
                        catch (ObjectDisposedException)
                        {
                            // Re-throw to outer handler for clean shutdown
                            throw;
                        }
                        catch (ThreadAbortException)
                        {
                            // Re-throw to outer handler for clean shutdown
                            throw;
                        }
                        catch (Exception ex)
                        {
                            QcLog.ChillLogger.LogExceptionExpOnly(ex, key: "UDP Listener unexpected: " + ex.GetType().Name);

                            if (client?.Client == null || !client.Client.IsBound)
                                return;
                        }
                    }

                    threadState = ThreadState.ExitedLoop;

                }
                catch (ThreadAbortException)
                {
                }
                catch (System.OperationCanceledException)
                {
                }
                catch (System.ObjectDisposedException)
                {
                }
                catch (System.Exception ex)
                {
                    Debug.LogError(Port.ToString() + "UDP Listener thread died unexpectedly: {0}-{1}".F(ex.GetType().Name, ex.Message));
                    Debug.LogException(ex);
                }
                finally
                {
                    threadState = ThreadState.ClosingLoop;

                    try
                    {
                        client.Close();
                        client.Dispose();
                    } catch (Exception ex)
                    {
                        Debug.LogError($"Error closing UDP client on port {Port} in finally block: {ex.Message}");
                        Debug.LogException(ex);
                    }
                    finally
                    {

                        UDPClient = null;
                        portIsListening = false;
                    }
                }
            }

            private static void SuppressUdpConnectionReset(Socket socket)
            {
                try
                {
                    const int SIO_UDP_CONNRESET = -1744830452;
                    socket.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, new byte[0]);
                }
                catch
                {
                    // Only supported by Windows UDP sockets. Other platforms can ignore it.
                }
            }

            #region Inspector

            public void Inspect()
            {
                "Listening: {0}. State: {1}".F(
                    portIsListening,
                    threadState).PL().NL();

                if (portIsListening && threadState!= ThreadState.Stopped && "Stop".PL().Click())
                {
                    threadState = ThreadState.Stopped;
                    SetThreadDirty();
                }

                if (threadState == ThreadState.Stopped && "Restart".PL().Click())
                {
                    threadState = ThreadState.NotStarted;
                    SetThreadDirty();
                }

                pegi.NL();
            }

            #endregion

        }




        public enum ApiRequestStateEnum
        {
            Idle,
            GenerationgRequest,
            WaitingResponse,
            ParsingResponse,
            RequestSucceeded,
            FailedResponseCode,

            Disposed,
            RequestFinished,
            GotWebResponse,
            Sending
        }

        public abstract class RequestBase
        {
            public abstract string Method { get; }
            public abstract string Path { get; }
            public virtual object? BodyObject { get; }
            public string Response = "";
            public bool IsSuccess;

        }

        public abstract class ApiBase<T> where T : RequestBase//struct, System.Enum
        {
            protected int _isRunning;
            // public T CurrentRequestType;
            protected CancellationTokenSource _cts = null!;

            protected abstract string BaseUrl { get; }

            public ApiRequestStateEnum CurrentState { get; protected set; }

            protected readonly ConcurrentQueue<T> _pendingRequests = new();
            protected readonly ConcurrentQueue<T> _responses = new();

            public void Enqueue(T request) => _pendingRequests.EnqueueClearIfFull(maxCount: 100, request);

            public void ManagedUpdate_Requests()
            {
                if (_responses.TryDequeue(out var response))
                {
                    try
                    {
                        ProcessResponse(response);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }

                if (Volatile.Read(ref _isRunning) != 0)
                    return;

                if (!_pendingRequests.TryDequeue(out var request))
                    return;

                Volatile.Write(ref _isRunning, 1);

                _ = RunRequestThreaded(request);
            }

            protected UnityWebRequest CreateRequest(string baseUrl, T requester) 
            {
                string url = baseUrl + requester.Path;

                UnityWebRequest request;

                if (requester.Method == UnityWebRequest.kHttpVerbGET)
                {
                    request = UnityWebRequest.Get(url);
                }
                else
                {
                    request = new UnityWebRequest(url, requester.Method)
                    {
                        downloadHandler = new DownloadHandlerBuffer()
                    };
                }

                return request;
            }

            protected abstract void ProcessResponse(T response);
            protected async Task RunRequestThreaded(T request)
            {
                try
                {
                    CurrentState = QcNet.ApiRequestStateEnum.GenerationgRequest;

                    using UnityWebRequest uwr = CreateRequest(BaseUrl, request);
                    SetupWebRequest(uwr, request);

                    CurrentState = QcNet.ApiRequestStateEnum.Sending;


                    if (Application.isEditor)
                        DebugRequest(uwr);
                    // Debug.Log("Sending request: " + uwr.us);

                    UnityWebRequestAsyncOperation op = uwr.SendWebRequest();

                    while (!op.isDone)
                        await Task.Yield();

                    CurrentState = QcNet.ApiRequestStateEnum.GotWebResponse;

                    string rawText = uwr.downloadHandler?.text ?? "";
                    long responseCode = uwr.responseCode;

                    if (responseCode < 200 || responseCode >= 300)
                    {
                        CurrentState = QcNet.ApiRequestStateEnum.FailedResponseCode;

                        Debug.LogError("HTTP request failed\n" +
                            "URL: " + BaseUrl + request.Path + "\n" +
                            "Code: " + responseCode + "\n" +
                            "UnityError: " + uwr.error + "\n" +
                            "Response: " + rawText);

                        return;
                    }

                    request.Response = rawText;
                    request.IsSuccess = true;

                    CurrentState = QcNet.ApiRequestStateEnum.ParsingResponse;
                    _responses.EnqueueClearIfFull(maxCount: 100, request);

                }
                catch (OperationCanceledException)
                {
                    Debug.LogError("Operation was cancelled");
                    CurrentState = QcNet.ApiRequestStateEnum.FailedResponseCode;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.LogError("An error occurred while processing the request {0}: {1}".F(request.ToString(), ex.Message));
                    CurrentState = QcNet.ApiRequestStateEnum.FailedResponseCode;
                }
                finally
                {
                    Interlocked.Exchange(ref _isRunning, 0);
                    CurrentState = QcNet.ApiRequestStateEnum.RequestFinished;
                }
            }

           
            private static void DebugRequest(UnityWebRequest request)
            {
                StringBuilder sb = new();

                sb.AppendLine("=== UNITY WEB REQUEST START ===");
                sb.AppendLine($"Method: {request.method}");
                sb.AppendLine($"URL: {request.url}");
                sb.AppendLine($"Timeout: {request.timeout}");

                string authHeader = request.GetRequestHeader("Authorization");
                string contentType = request.GetRequestHeader("Content-Type");

                if (!string.IsNullOrEmpty(contentType))
                    sb.AppendLine($"Content-Type: {contentType}");

                if (!string.IsNullOrEmpty(authHeader))
                    sb.AppendLine("Authorization: <hidden>");

                Debug.Log(sb.ToString());
            }


            protected abstract void SetupWebRequest(UnityWebRequest web, T request);
        }
    }
}
