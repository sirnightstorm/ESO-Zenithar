using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WinFormsApp;

namespace ZenitharClient.Src
{
    public static class JSONUploader
    {
        //private const string ApiUrl = "https://api.moraswhispers.com/api/ingest/batch";

        private static string ComputeSignature(string secret, long timestampSeconds, string rawBody)
        {
            string message = $"{timestampSeconds}.{rawBody}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        internal static async Task Process(Queue<JSONTransaction> txnQueue, string language, TrayApplicationContext context)
        {
            while (txnQueue.Count > 0)
            {
                var batch = TakeFromQueue(txnQueue, 200).ToList();
                try
                {
                    context.SetTooltip($"Uploading {batch.Count} " + (batch.Count != 1 ? "transactions" : "transaction") + $" ({txnQueue.Count} left)");
                    await SendBatch(batch, language);

                    if (txnQueue.Count > 0)
                    {
                        LogForm.Log("Waiting 5s");
                        await Task.Delay(5000);
                    }
                }
                catch (Exception ex)
                {
                    LogForm.Log($"Error sending batch: {ex.Message}");
                    // Re-enqueue failed transactions
                    foreach (var item in batch)
                    {
                        txnQueue.Enqueue(item);
                    }
                    LogForm.Log("Requeued transactions. Waiting 60s");
                    await Task.Delay(60000);
                }
            }
            LogForm.Log("Queue is empty");
        }

        internal static async Task SendBatch(List<JSONTransaction> txnList, string language)
        {
            String guildToken = Properties.Settings.Default.GuildToken;
            String endpoint = Properties.Settings.Default.ServerEndpoint;


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
