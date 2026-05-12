#nullable enable

using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace QuizCanners.Utils
{
    public static partial class QcNet
    {
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
