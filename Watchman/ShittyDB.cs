using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watchman
{
    public static class ShittyDB
    {
        public static ShittyDBData? Current { get; private set; } = new();

        private static object _lock = new();

        public static void Load()
        {
            lock (_lock)
            {
                if (!File.Exists("./external/.shittydb"))
                {
                    File.Create("./external/.shittydb");
                    Current = new();
                    return;
                }

                Current = JsonConvert.DeserializeObject<ShittyDBData>(File.ReadAllText("./external/.shittydb"));
            }
        }

        public static void Modify(Action<ShittyDBData?> func)
        {
            func(Current);
            Save();
        }

        public static void Save(ShittyDBData? data = null)
        {
            lock (_lock)
            {
                data ??= Current;
                File.WriteAllText("./external/.shittydb", JsonConvert.SerializeObject(data, Formatting.Indented));
                Current = data;
            }
        }
    }

    public class ShittyDBData
    {
        [JsonProperty("pending")]
        public List<Verification> Pending { get; set; } = new();

        [JsonProperty("approved")]
        public List<Verification> Approved { get; set; } = new();

        public (Verification Verification, string State)? Get(ulong userId)
        {
            var result = Pending.FirstOrDefault(x => x.UserId == userId);

            if(result != null)
            {
                return (result, "Pending");
            }

            result = Approved.FirstOrDefault(x => x.UserId == userId);

            if(result != null)
            {
                return (result, "Approved");
            }

            return null;
        }
    }

    public class Verification
    {
        [JsonProperty("userId")]
        public ulong UserId { get; set; }
        [JsonProperty("verifiedBy")]
        public ulong? VerifiedBy { get; set; }
        [JsonProperty("reason")]
        public string? Reason { get; set; }
    }
}
