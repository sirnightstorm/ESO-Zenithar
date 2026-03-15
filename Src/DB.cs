using Microsoft.Data.Sqlite;

namespace ZenitharClient.Src
{
    internal class DB : Service
    {
        private SqliteConnection connection;

        internal DB(string dbPath) : base("Database")
        {
            // Ensure the directory for the database exists
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();
            CreateSchema();

            _ = UpdateState();
        }

        private void CreateSchema()
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS transactions (
                    eventId INTEGER PRIMARY KEY,
                    timestamp INTEGER NOT NULL,
                    account TEXT NOT NULL,
                    type TEXT NOT NULL,
                    item TEXT NOT NULL,
                    quantity INTEGER,
                    gold INTEGER NOT NULL,
                    itemLink TEXT,
                    accountRank INTEGER,
                    uploaded INTEGER DEFAULT 0
                );
                /* Index to list not-uploaded transactions, by timestamp */
                CREATE INDEX IF NOT EXISTS idx_transactions_uploaded_timestamp ON transactions (uploaded, timestamp);
            ";
            cmd.ExecuteNonQuery();
        }

        internal async Task UpdateState()
        {
            var unuploadedCount = await GetUnuploadedTransactionCount();
            var count = await GetTransactionCount();

            if (unuploadedCount > 0)
            {
                SetState(ServiceState.Waiting, $"{unuploadedCount} " + (unuploadedCount != 1 ? "transactions" : "transaction"));
            }
            else if (count > 0)
            {
                SetState(ServiceState.Waiting, "Waiting to clear");
            }
            else
            {
                SetState(ServiceState.Idle, "Empty");
            }
        }

        internal async Task Begin()
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "BEGIN TRANSACTION;";
            await cmd.ExecuteNonQueryAsync();
        }

        internal async Task Commit()
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "COMMIT;";
            await cmd.ExecuteNonQueryAsync();
        }

        internal async Task InsertTransaction(
            long eventId,
            long timestamp,
            string account,
            string type,
            string item,
            int? quantity,
            int gold,
            string? itemLink,
            int? accountRank
        )
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR IGNORE INTO transactions (eventId, timestamp, account, type, item, quantity, gold, itemLink, accountRank)
                VALUES ($eventId, $timestamp, $account, $type, $item, $quantity, $gold, $itemLink, $accountRank);
            ";
            cmd.Parameters.AddWithValue("$eventId", eventId);
            cmd.Parameters.AddWithValue("$timestamp", timestamp);
            cmd.Parameters.AddWithValue("$account", account);
            cmd.Parameters.AddWithValue("$type", type);
            cmd.Parameters.AddWithValue("$item", item);
            cmd.Parameters.AddWithValue("$quantity", (object?)quantity ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$gold", gold);
            cmd.Parameters.AddWithValue("$itemLink", (object?)itemLink ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$accountRank", (object?)accountRank ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();

            await UpdateState();
        }

        internal async Task<List<JSONTransaction>> GetUnuploadedTransactions(int batchSize)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT eventId, timestamp, account, type, item, quantity, gold, itemLink, accountRank
                FROM transactions
                WHERE uploaded = 0
                ORDER BY timestamp ASC
                LIMIT $batchSize;
            ";
            cmd.Parameters.AddWithValue("$batchSize", batchSize);
            var result = new List<JSONTransaction>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new JSONTransaction
                {
                    eventId = reader.GetInt64(0),
                    epoch = reader.GetInt64(1),
                    account = reader.GetString(2),
                    type = reader.GetString(3),
                    item = reader.GetString(4),
                    quantity = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    gold = reader.GetInt32(6),
                    itemLink = reader.IsDBNull(7) ? null : reader.GetString(7),
                    accountRank = reader.IsDBNull(8) ? null : reader.GetInt32(8)
                });
            }
            return result;
        }

        internal async Task<int> GetUnuploadedTransactionCount()
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*)
                FROM transactions
                WHERE uploaded = 0;
            ";
            var count = await cmd.ExecuteScalarAsync();
            return (int)((long)(count ?? 0));
        }

        internal async Task<int> GetTransactionCount()
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*)
                FROM transactions;
            ";
            var count = await cmd.ExecuteScalarAsync();
            return (int)((long)(count ?? 0));
        }

        internal async Task MarkTransactionsAsUploaded(List<JSONTransaction> transactions)
        {
            if (transactions == null || transactions.Count == 0)
                return;

            var eventIds = transactions.Select(t => t.eventId).Distinct().ToList();
            if (eventIds.Count == 0)
                return;

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE transactions
                SET uploaded = 1
                WHERE eventId IN (" + string.Join(",", eventIds) + @");
            ";
            await cmd.ExecuteNonQueryAsync();

            await UpdateState();
        }

        internal async Task RemoveUploadedTransactions()
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                DELETE FROM transactions
                WHERE uploaded = 1;
                VACUUM;
            ";
            await cmd.ExecuteNonQueryAsync();

            await UpdateState();
        }
    }
}