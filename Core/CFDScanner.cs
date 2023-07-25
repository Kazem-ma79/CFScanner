using System.Diagnostics;
using System.Net;

namespace Core
{
    public class ScanResult
    {
        public enum ScanStatus
        {
            Success,
            Filtered,
            Timeout,
            Error
        }

        public ScanStatus Status;

        public long Delay;
    }
    public class CFDScanner
    {
        public static async Task<ScanResult> Scan(string hostname, int port, string ip, string path = "/", int timeout = 5)
        {
            string proto = port switch
            {
                443 or 2053 or 2083 or 2087 or 2096 or 8443 => "https",
                _ => "http",
            };
            string url = port == 80 ? $"{proto}://{ip}" : port == 443 ? $"{proto}://{ip}" : $"{proto}://{ip}:{port}";
            url += path;
            long delay = -1;

            HttpClientHandler httpClientHandler = new()
            {
                AllowAutoRedirect = false
            };

            using HttpClient client = new(httpClientHandler)
            {
                BaseAddress = new Uri(url),
                Timeout = TimeSpan.FromSeconds(timeout)
            };
            client.DefaultRequestHeaders.Host = hostname;
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var result = await client.GetAsync(url);
                var statusCode = result.StatusCode;
                string resultContent = await result.Content.ReadAsStringAsync();
                delay = stopwatch.ElapsedMilliseconds;
                stopwatch.Stop();
                bool isNormal = statusCode == HttpStatusCode.OK;
                bool isV2ray = statusCode == HttpStatusCode.BadRequest && !resultContent.ToLower().Contains("plain http request was sent to https port");
                bool isFiltered = (statusCode == HttpStatusCode.MovedPermanently || statusCode == HttpStatusCode.Found) && result.Headers.Location.Host.Contains("10.10.34.3");
                return new()
                {
                    Delay = delay,
                    Status = isV2ray || isNormal ? ScanResult.ScanStatus.Success : isFiltered ? ScanResult.ScanStatus.Filtered : ScanResult.ScanStatus.Error
                };
            }
            catch (Exception ex)
            {
                if (ex is TimeoutException || ex is TaskCanceledException)
                    return new()
                    {
                        Delay = delay,
                        Status = ScanResult.ScanStatus.Timeout
                    };
                return new()
                {
                    Delay = delay,
                    Status = ScanResult.ScanStatus.Error
                };
            }
        }
    }
}
