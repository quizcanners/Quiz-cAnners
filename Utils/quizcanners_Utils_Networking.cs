using QuizCanners.Inspect;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Security.Cryptography;

namespace QuizCanners.Utils
{
    public static partial class QcNet
    {
        public static partial class Cryptography
        {
            public static string GenerateUrlSafeSecret(int byteCount = 32)
            {
                byte[] bytes = new byte[byteCount];
                RandomNumberGenerator.Fill(bytes);

                return Convert.ToBase64String(bytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");
            }

            internal static void Inspect()
            {
                if ("Generate URL-safe secret".PL().Click().NL())
                {
                    string secret = GenerateUrlSafeSecret();

                    Debug.Log(secret);

                    pegi.CopyPasteBuffer = secret;
                }
            }   
        }


        //private static DateTime syncedUtcTimeAtStartup;
        private static bool IsSynced;
        private static readonly TimeThreadManager _timeRequestManager = new();
        static double SINCE_GAME_START_SECONDS => Time.realtimeSinceStartupAsDouble;

        public static bool IsNewerThan(this TimeStamp me, TimeStamp other)
        {
            return other == null || (me != null && (me.GetUTC_Miliseconds() > other.GetUTC_Miliseconds()));
        }

        public static bool IsOlderThan(this TimeStamp me, TimeStamp other)
        {
            return me == null || (other!= null && (other.GetUTC_Miliseconds() > me.GetUTC_Miliseconds()));
        }

        [Serializable]
        public class TimeStamp : IPEGI, IPEGI_ListInspect
        {
            [SerializeField] public bool _gotSynchronizedData;
            [SerializeField] public long _cachedUTCAtCreation;

            
            private void UpdateString() 
            {
               // _longAsString = _cachedUTCAtCreation.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            public bool IsValid => _cachedUTCAtCreation > 0;

    

            public void SetUTC(DateTime dt)
            {
                // Treat Unspecified as UTC (common when deserializing), and convert Local -> UTC
                if (dt.Kind == DateTimeKind.Unspecified)
                    dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                else if (dt.Kind == DateTimeKind.Local)
                    dt = dt.ToUniversalTime();

                _cachedUTCAtCreation = new DateTimeOffset(dt).ToUnixTimeMilliseconds();
                _gotSynchronizedData = true;
                UpdateString();
            }

            public DateTime GetUTC() => DateTimeOffset.FromUnixTimeMilliseconds(GetUTC_Miliseconds()).UtcDateTime;

            public TimeSpan GetTimePassed() => _timeRequestManager.UtcNow() - GetUTC();


            private const long Days265InMilliseconds = 1000L * 60L * 60L * 24L * 265L;

            public long GetUTC_Miliseconds() 
            {
                if (_cachedUTCAtCreation == 0) 
                {
                    /*if (!_longAsString.IsNullOrEmpty())
                    {
                        if (long.TryParse(_longAsString, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out _cachedUTCAtCreation))
                            Debug.Log($"Parsed UTC time: {_cachedUTCAtCreation}");
                        else
                            Debug.LogError("Failed to parse " + _longAsString);
                    }
                    else
                    {*/
                        _cachedUTCAtCreation = _timeRequestManager.UtcNow_Miliseconds_LocalUnsynched - Days265InMilliseconds;
                        UpdateString();
                   // }
                    
                }

                if (!_gotSynchronizedData) 
                {
                    _gotSynchronizedData = _timeRequestManager.TryGetSynchedUTCNow_Miliseconds(out var _);
                    if (_gotSynchronizedData)
                    {
                        _cachedUTCAtCreation += _timeRequestManager.SynchedMinusLocal_Miliseconds();
                        UpdateString();
                    }
                }

                return _cachedUTCAtCreation;
            }

            public void SetTimestampNow() 
            {
                _gotSynchronizedData = _timeRequestManager.TryGetSynchedUTCNow_Miliseconds(out _cachedUTCAtCreation);
                UpdateString();
            }

            public void CopyFrom(TimeStamp ts) 
            { 
                _cachedUTCAtCreation = ts._cachedUTCAtCreation;
                _gotSynchronizedData = ts._gotSynchronizedData;
                UpdateString();
            }

            private void SetTimestampUnsinchronizedNow()
            {
                _gotSynchronizedData = false;
                _cachedUTCAtCreation = _timeRequestManager.UtcNow_Miliseconds_LocalUnsynched;
                UpdateString();
            }

            public TimeStamp() {}

            public TimeStamp(DateTime dateUpdateUtc)
            {
                SetUTC(dateUpdateUtc);
            }

            public static TimeStamp CreateCurrentTimeStamp()
            {
                TimeStamp ts = new();
                ts.SetTimestampNow();
                return ts;
            }

            public static TimeStamp CreateUnsynchronizedTimestamp()
            {
                TimeStamp ts = new();
                ts.SetTimestampNow();
                return ts;
            }

            #region Inspector

            public override string ToString() => GetTimePassed().ToShortDisplayString();

            public void Inspect()
            {
                ToString().NL();

                var dt = GetUTC();

                dt.ToRelativeString(showHours: true).NL();

                "Creating time".ConstL().Edit(ref dt).NL(()=>SetUTC(dt));

                "Synchronized: {0}".F(_gotSynchronizedData).NL();
                "Creation time: {0}".F(GetUTC()).NL();
                if ("Set Unsynchronized".PL().Click().NL())
                {
                    SetTimestampUnsinchronizedNow();
                }

                _timeRequestManager.Nested_Inspect();
            }

            public pegi.ChangesToken InspectShort(string name) 
            {
                var changed = pegi.ChangeTrackStart();

                var dt = GetUTC();
                name.ConstL().Edit(ref dt).OnChanged(()=> SetUTC(dt));

                return changed;
            }

            public void InspectInList(ref int edited, int index)
            {

                if (Icon.Enter.Click())
                    edited = index;

                var dt = GetUTC();

                pegi.Edit(ref dt).NL(() => SetUTC(dt));

             //   ToString().PL().Write();

                if (Icon.Refresh.Click())
                    SetTimestampNow();
            }

         

            #endregion
        }

        private class TimeThreadManager : IPEGI
        {
            private const string timeApiUrl = "http://worldtimeapi.org/api/timezone/Etc/UTC";

            public enum JobState { Uninitialized, Running, ThreadRawReady, ProcessedResult, Failed }
            public volatile JobState _state = JobState.Uninitialized;
          //  private Thread _thread;

            private Exception _error;
            private System.Diagnostics.Stopwatch _stopwatchFromThreadStartToRequestReturn;
            private double unityTimeAtStopwatchStart_Seconds;
            private long _unixAtGameStart_Miliseconds;
            private long _unixTimeWhenRequestWasReceived_Miliseconds;
            private long _utcNow_Miliseconds_Cached; // => ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
            private readonly Gate.Frame _utsNowFrameGate = new();

            //  public static long FromUnixNow_Miliseconds => ((DateTimeOffset)GetUtcSynchronizedNow()).ToUnixTimeMilliseconds();

            private readonly Gate.Frame _frameGate = new();

            internal long SynchedMinusLocal_Miliseconds()
            {
                TryGetSynchedUTCNow_Miliseconds(out var utcNow);
                return utcNow - UtcNow_Miliseconds_LocalUnsynched;
            }

            public DateTime UtcNow()
            {
                TryGetSynchedUTCNow_Miliseconds(out var utcNow);
                return DateTimeOffset.FromUnixTimeMilliseconds(utcNow).UtcDateTime;
            }

            public long UtcNow_Miliseconds_LocalUnsynched => ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();



            public bool TryGetSynchedUTCNow_Miliseconds(out long utcNow_Miliseconds)
            {
                utcNow_Miliseconds = GetUtcSynchronizedNow();
                return IsSynced;


                long GetUtcSynchronizedNow()
                {
                    if (!_utsNowFrameGate.TryConsume())
                        return _utcNow_Miliseconds_Cached;

                    if (!IsSynced)
                    {
                        if (TryGet(out long _unixAtGameStart_Miliseconds) == TimeThreadManager.JobState.ProcessedResult)
                        {
                            IsSynced = true;
                            _utcNow_Miliseconds_Cached = (long)Math.Round(_unixAtGameStart_Miliseconds + (SINCE_GAME_START_SECONDS * 1000));
                        }
                        else
                            _utcNow_Miliseconds_Cached = UtcNow_Miliseconds_LocalUnsynched;
                    }
                    else 
                        _utcNow_Miliseconds_Cached = (long)Math.Round(_unixAtGameStart_Miliseconds + (SINCE_GAME_START_SECONDS * 1000));

                    return _utcNow_Miliseconds_Cached;
                }
            }

            public void Inspect()
            {
                "State: {0}".F(_state).PL().Write();

                if ("Start".PL().Click().NL())
                {
                    StartThread();
                }

                if (_stopwatchFromThreadStartToRequestReturn != null)
                    "Elapsed: {0} ms".F(_stopwatchFromThreadStartToRequestReturn.Elapsed.TotalMilliseconds).NL();

                "Unity time at Stopwatch Start: {0} s".F(QcSharp.SecondsToReadableString(unityTimeAtStopwatchStart_Seconds)).NL();
                "Delta with Unsynched: {0}".F(SynchedMinusLocal_Miliseconds()).NL();
            }

            private readonly Gate.UnityTimeUnScaled _timeRequestTask = new();

            public JobState TryGet(out long value)
            {
                switch (_state)
                {
                    case JobState.Failed:
                    case JobState.Uninitialized:
                        if (_timeRequestTask.TryConsume_IfElapsedOrFirst(10))
                        {
                            StartThread();
                        }
                        value = default;
                        return _state;
                    case JobState.Running:
                        value = default;
                        return _state;
                    case JobState.ThreadRawReady:

                        _unixAtGameStart_Miliseconds = _unixTimeWhenRequestWasReceived_Miliseconds - (int)Math.Floor(unityTimeAtStopwatchStart_Seconds * 1000 + _stopwatchFromThreadStartToRequestReturn.Elapsed.TotalMilliseconds);

                        goto case JobState.ProcessedResult;
                    case JobState.ProcessedResult:
                        _state = JobState.ProcessedResult;
                        value = _unixAtGameStart_Miliseconds;
                        return _state;

                    default:
                        value = default;
                        return _state;
                }
            }

            static readonly HttpClient client = new()
            {
                Timeout = TimeSpan.FromSeconds(5)
            };

            private void StartThread()
            {
                _state = JobState.Running;

                _stopwatchFromThreadStartToRequestReturn = System.Diagnostics.Stopwatch.StartNew();
                unityTimeAtStopwatchStart_Seconds = Time.realtimeSinceStartupAsDouble;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        _unixTimeWhenRequestWasReceived_Miliseconds = await GetUnixTimeMsAsync();
                        _state = JobState.ThreadRawReady;

                       // Debug.Log("Time synchronized successfully. UTC Unix Time (ms): " + _unixTimeWhenRequestWasReceived_Miliseconds);

                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        _error = ex;
                        _state = JobState.Failed;
                    }
                });


                /*
                static async Task<long> GetUnixTimeAsync_Milliseconds()
                {
                    using var resp = await client.GetAsync(timeApiUrl, HttpCompletionOption.ResponseHeadersRead);
                    resp.EnsureSuccessStatusCode();

                    string json = await resp.Content.ReadAsStringAsync();

                    var response = JsonUtility.FromJson<TimeApiResponse>(json);
                    return response.unixtime * 1000L;
                }*/

                /*
                async Task<long> GetUnixTimeAsync_Miliseconds()
                {
                    using (HttpClient client = new())
                    {
                        string json = await client.GetStringAsync(timeApiUrl);
                        _stopwatchFromThreadStartToRequestReturn.Stop();

                        var response = JsonUtility.FromJson<TimeApiResponse>(json);

                        return response.unixtime * 1000;
                    }
                }*/
            }

            private const ulong UnixEpochInNtpSeconds = 2208988800UL;

            /// <summary>Returns UTC Unix time in milliseconds from an NTP server.</summary>
            public static async Task<long> GetUnixTimeMsAsync(
                string host = "pool.ntp.org",
                int timeoutMs = 5000,
                CancellationToken ct = default)
            {
                // 48-byte NTP request packet
                // LI = 0 (no warning), VN = 4, Mode = 3 (client) => 0b00_100_011 = 0x23
                byte[] request = new byte[48];
                request[0] = 0x23;

                // Resolve host -> IP
                IPAddress[] ips = await Dns.GetHostAddressesAsync(host);
                if (ips == null || ips.Length == 0)
                    throw new SocketException((int)SocketError.HostNotFound);

                // Prefer IPv4 first (many networks handle it more consistently)
                IPAddress ip = null;
                for (int i = 0; i < ips.Length; i++)
                {
                    if (ips[i].AddressFamily == AddressFamily.InterNetwork) { ip = ips[i]; break; }
                }
                ip ??= ips[0];

                var endpoint = new IPEndPoint(ip, 123);

                using var udp = new UdpClient(ip.AddressFamily);
                udp.Connect(endpoint);

                // Send request
                await udp.SendAsync(request, request.Length);

                // Receive with timeout + cancellation
                var receiveTask = udp.ReceiveAsync();
                var delayTask = Task.Delay(timeoutMs, ct);

                var finished = await Task.WhenAny(receiveTask, delayTask);
                if (finished != receiveTask)
                {
                    ct.ThrowIfCancellationRequested();
                    throw new TimeoutException($"NTP timeout after {timeoutMs}ms (host: {host}, ip: {ip}).");
                }

                UdpReceiveResult result = receiveTask.Result;
                byte[] response = result.Buffer;

                if (response == null || response.Length < 48)
                    throw new InvalidOperationException("Invalid NTP response (too short).");

                // Transmit Timestamp is at byte 40..47 (seconds, fraction), big-endian.
                ulong seconds = ReadUInt32BE(response, 40);
                ulong fraction = ReadUInt32BE(response, 44);

                // Convert NTP time -> Unix ms
                if (seconds < UnixEpochInNtpSeconds)
                    throw new InvalidOperationException("NTP time is before Unix epoch (bad server response).");

                ulong unixSeconds = seconds - UnixEpochInNtpSeconds;
                // fraction is 32-bit fraction of a second
                double fractionalMs = (fraction * 1000.0) / 4294967296.0; // 2^32

                long unixMs = checked((long)(unixSeconds * 1000UL + (ulong)fractionalMs));
                return unixMs;
            }

            private static uint ReadUInt32BE(byte[] buffer, int offset)
            {
                return (uint)(
                    (buffer[offset + 0] << 24) |
                    (buffer[offset + 1] << 16) |
                    (buffer[offset + 2] << 8) |
                    (buffer[offset + 3] << 0)
                );
            }

            [Serializable]
            private class TimeApiResponse
            {
                public long unixtime;
            }
        }
    }
}