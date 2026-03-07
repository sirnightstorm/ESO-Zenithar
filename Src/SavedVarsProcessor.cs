using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ZenitharClient.Src
{
    /*
      eventId: 123456,
      epoch: 1772191990,
      account: "@PlayerOne",
      type: "deposit",
      item: "Rubedite Ingot",
      quantity: 50,
      gold: 1000,

     */
    public class JSONTransaction
    {
        public required long eventId { get; set; }
        public required long epoch { get; set; }
        public required string account { get; set; }
        public required string type { get; set; }
        public required string item { get; set; }
        public int? quantity { get; set; }
        public required int gold { get; set; }
        public string? itemLink { get; set; }
        public int? accountRank { get; set; }
    }

    public static class SavedVarsProcessor
    {
        public static long guildId = 767808;
     
        public static void Process(LuaDataRoot dataRoot, Queue<JSONTransaction> txnQueue, out string language, TrayApplicationContext context)
        {
            language = "en";

            foreach (var (accountName, characterMap) in dataRoot.Default)
            {
                var accountWide = characterMap["$AccountWide"];
                language = accountWide.language;

                if (accountWide.guilds.TryGetValue($"guild:{guildId}", out var guild))
                {
                    var guildData = guild.Deserialize<LuaGuild>();

                    if (guildData != null)
                    {
                        foreach (var (txnId, txn) in guildData.txns)
                        {
                            Debug.WriteLine(txnId);
                            var user = guildData.users.FirstOrDefault(u => u.Value.id == txn.user);

                            var item = guildData.items.FirstOrDefault(i => i.Value.id == txn.item);
                            var jsonTxn = new JSONTransaction
                            {
                                eventId = txnId,
                                epoch = txn.ts,
                                type = txn.gold > 0 ? "deposit" : "withdrawal",
                                item = item.Value?.name ?? "Gold",
                                quantity = txn.qty != 0 ? Math.Abs(txn.qty) : null,
                                gold = txn.gold,
                                itemLink = item.Key,
                                account = user.Key,
                                accountRank = user.Value.rankIndex
                            };
                            txnQueue.Enqueue(jsonTxn);
                        }
                    }
                }

                Console.WriteLine($"Key = {accountName}, Value = {accountWide}");
            }
        }
    }
}
