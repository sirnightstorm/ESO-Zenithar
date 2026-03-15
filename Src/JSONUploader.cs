using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ZenitharClient.Src
{
    public class JSONUploader : Service
    {
        private static JSONUploader? instance = null;
        private static readonly object padlock = new object();

        private JSONUploader() : base("Uploader")
        {
        }

        public static JSONUploader Instance

        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new JSONUploader();
                    }
                    return instance;
                }
            }
        }

        private string ComputeSignature(string secret, long timestampSeconds, string rawBody)
        {
            string message = $"{timestampSeconds}.{rawBody}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        internal async Task Process(DB db, string language, TrayApplicationContext context)
        {
            if (State != ServiceState.Idle)
            {
                return;
            }

            SetState(ServiceState.Active, "Uploading...");
            while (true)
            {
                var batch = await db.GetUnuploadedTransactions(200);
                if (batch.Count == 0)
                {
                    break;
                }
                var totalTxns = await db.GetUnuploadedTransactionCount();

                try
                {
                    SetState(State, $"Uploading {batch.Count} " + (batch.Count != 1 ? "transactions" : "transaction"));

                    await SendBatch(batch, language);

                    await db.MarkTransactionsAsUploaded(batch);

                    if (await db.GetUnuploadedTransactionCount() > 0)
                    {
                        SetState(ServiceState.Active, $"Uploaded {batch.Count} " + (batch.Count != 1 ? "transactions" : "transaction"));
                        await Task.Delay(1000);
                    }
                }
                catch (Exception ex)
                {
                    LogForm.Log($"Error sending batch: {ex.Message}");

                    SetState(ServiceState.Error, "Error uploading transactions");

                    LogForm.Log("Waiting 60s");
                    await Task.Delay(60000);
                }
            }
            LogForm.Log("Queue is empty");

            SetState(ServiceState.Idle);
        }

        internal async Task SendBatch(List<JSONTransaction> txnList, string language)
        {
            string? guildToken = Program.config.GuildToken;
            string? endpoint = Program.config.ServerEndpoint;


            if (string.IsNullOrWhiteSpace(guildToken) || string.IsNullOrWhiteSpace(endpoint))
                throw new Exception("Set GuildToken and ServerEndpoint.");

            var payload = new
            {
                schemaVersion = 1,
                sentAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                source = "Zenithar",
                language,
                transactions = txnList
            };

            // IMPORTANT: raw JSON string must be signed exactly as sent

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            string rawBody = JsonSerializer.Serialize(payload, options);

            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string sig = ComputeSignature(Program.ClientSecret, ts, rawBody);

            using var client = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(rawBody, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", guildToken);
            request.Headers.Add("X-Client-Secret", Program.ClientSecret);
            request.Headers.Add("X-Timestamp", ts.ToString());
            request.Headers.Add("X-Signature", sig);

            var response = await client.SendAsync(request);
            string text = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"HTTP {(int)response.StatusCode}");
            Debug.WriteLine(text);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Request failed: HTTP {(int)response.StatusCode}");
        }

        static IEnumerable<JSONTransaction> TakeFromQueue(Queue<JSONTransaction> q, int max)
        {
            while (max-- > 0 && q.Count > 0)
                yield return q.Dequeue();
        }

    }
}
