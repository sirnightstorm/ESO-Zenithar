using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;


namespace ZenitharClient.Src
{
    public class LuaDataRoot
    {
        public Dictionary<string, Dictionary<string, LuaCharacter>> Default { get; set; } = new();
    }

    //public class ZenitharAccount
    //{
    //    [JsonPropertyName("$AccountWide")]
    //    public ZenitharAccountWide AccountWide { get; set; } = new();
    //}

    public class LuaCharacter
    {
        public int version { get; set; }
        public int processed { get; set; }
        public required string language { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> guilds { get; set; } = new();
    }
    
    public class LuaGuild
    {
        public Dictionary<string, LuaUser> users { get; set; } = new();
        public Dictionary<long, LuaTxn> txns { get; set; } = new();
        public Dictionary<string, LuaItem> items { get; set; } = new();
        public Dictionary<int, long> lastEvent { get; set; } = new();
        public LuaIds ids { get; set; } = new();
    }

    public class LuaTxn
    {
        public long ts { get; set; }
        public int qty { get; set; }
        public int gold { get; set; }
        public int item { get; set; }
        public int user { get; set; }
    }

    public class LuaUser
    {
        public int id { get; set; }
        //public long initialScan { get; set; }
        public int rankIndex { get; set; }
    }

    public class LuaItem
    {
        public string icon { get; set; } = "";
        public int id { get; set; }
        public string name { get; set; } = "";
    }

    public class LuaIds
    {
        public int user { get; set; }
        public int item { get; set; }
    }

    public static class SavedVarsParser
    {
        private static string Convert(string lua)
        {
            // Remove comments
            lua = Regex.Replace(lua, @"--.*?$", "", RegexOptions.Multiline);

            // Remove top-level variable assignment (e.g., Zenithar_data =)
            lua = Regex.Replace(lua, @"^\s*\w+\s*=\s*", "", RegexOptions.Multiline);

            // First, normalize keys and brackets with regexes that don't touch values
            // Convert ["key"] =  to "key":
            lua = Regex.Replace(lua, @"\[\s*""([^""]+)""\s*\]\s*=", "\"$1\":");
            // Convert [123] =  to "123":
            lua = Regex.Replace(lua, @"\[\s*(\d+)\s*\]\s*=", "\"$1\":");
            // Convert bareword keys (guilds, users, etc.) to "guilds":
            lua = Regex.Replace(lua, @"(?m)^\s*(\w+)\s*=", "\"$1\":");

            // Now do a careful pass to replace remaining '=' with ':' only outside strings
            var sb = new StringBuilder(lua.Length);
            bool inString = false;
            char stringDelim = '\0';

            for (int i = 0; i < lua.Length; i++)
            {
                char c = lua[i];

                if (inString)
                {
                    sb.Append(c);

                    // End of string?
                    if (c == stringDelim)
                    {
                        inString = false;
                    }
                    // Handle escaped quotes \" inside strings
                    else if (c == '\\' && i + 1 < lua.Length && lua[i + 1] == stringDelim)
                    {
                        sb.Append(lua[i + 1]);
                        i++;
                    }
                }
                else
                {
                    // Start of string?
                    if (c == '"' || c == '\'')
                    {
                        inString = true;
                        stringDelim = c;
                        sb.Append(c);
                    }
                    else if (c == '=')
                    {
                        // Treat as JSON colon
                        sb.Append(':');
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            var jsonLike = sb.ToString();

            // Remove trailing commas before } or ]
            jsonLike = Regex.Replace(jsonLike, @",(\s*[}\]])", "$1");

            // At this point, the structure is JSON-compatible
            return jsonLike.Trim();
        }

        private static string ExtractLuaBlock(string lua, string blockName)
        {
            var pattern = blockName + @"\s*=";
            var match = Regex.Match(lua, pattern);
            if (!match.Success)
                throw new Exception($"Block '{blockName}' not found.");

            int start = lua.IndexOf('{', match.Index);
            if (start < 0)
                throw new Exception($"Opening brace for '{blockName}' not found.");

            int depth = 0;
            bool inString = false;
            char stringDelim = '\0';

            for (int i = start; i < lua.Length; i++)
            {
                char c = lua[i];

                if (inString)
                {
                    if (c == stringDelim)
                        inString = false;
                    else if (c == '\\' && i + 1 < lua.Length)
                        i++;
                }
                else
                {
                    if (c == '"' || c == '\'')
                    {
                        inString = true;
                        stringDelim = c;
                    }
                    else if (c == '{')
                    {
                        depth++;
                    }
                    else if (c == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            return lua.Substring(start, i - start + 1);
                        }
                    }
                }
            }

            throw new Exception($"Block '{blockName}' is not properly closed.");
        }

        public static LuaDataRoot? ParseSavedVars(string svFilePath)
        {
            if (!File.Exists(svFilePath))
                return null;

            var lua = File.ReadAllText(svFilePath);

            if (string.IsNullOrWhiteSpace(lua))
                return null;

            var dataBlock = ExtractLuaBlock(lua, "Zenithar_data");
            var json = Convert(dataBlock);
            return JsonSerializer.Deserialize<LuaDataRoot>(json) ?? new LuaDataRoot();
        }

        public static bool IsProcessed(LuaDataRoot dataRoot)
        {
            foreach (var (accountName, characterMap) in dataRoot.Default)
            {
                var accountWide = characterMap["$AccountWide"];
                if (accountWide.processed == 0)
                {
                    return false;
                }
            }

            return true;
        }

        public static void SetProcessed(string svFilePath)
        {
            var original = File.ReadAllText(svFilePath);

            string updated = original.Replace("[\"processed\"] = 0", "[\"processed\"] = 1");

            File.WriteAllText(svFilePath, updated);
        }
    }
}
