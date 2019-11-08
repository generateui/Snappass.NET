using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Snappass
{
    public interface IMemoryStore
    {
        public bool Has(string key);
        public void Store(string encryptedPassword, string key, TimeToLive timeToLive);
        public string Retrieve(string key);
    }
    public sealed class MemoryStore : IMemoryStore
    {
        private class Item
        {
            public DateTime StoredDateTime { get; set; }
            public TimeToLive TimeToLive { get; set; }
            public string Key { get; set; }
            public string EncryptedPassword { get; set; }
        }

        private readonly Dictionary<string, Item> _items = new Dictionary<string, Item>();
        private readonly ILogger<MemoryStore> _logger;

        public MemoryStore(ILogger<MemoryStore> logger)
        {
            _logger = logger;
        }

        public bool Has(string key) => _items.ContainsKey(key);

        public void Store(string encryptedPassword, string key, TimeToLive timeToLive) 
        {
            var item = new Item
            {
                StoredDateTime = DateTime.Now,
                TimeToLive = timeToLive,
                EncryptedPassword = encryptedPassword,
                Key = key
            };
            _items.Add(key, item);
        }

        public string Retrieve(string key) 
        {
            if (key == null)
            {
                _logger.Log(LogLevel.Warning, $@"Tried to retrieve null key");
                return null;
            }
            if (!_items.ContainsKey(key))
            {
                _logger.Log(LogLevel.Warning, $@"Tried to retrieve password for unknown key [{key}]");
                return null;
            }
            var item = _items[key];
            DateTime GetAtTheLatest(TimeToLive ttl) => ttl switch
            {
                TimeToLive.Day => item.StoredDateTime.AddDays(1),
                TimeToLive.Week => item.StoredDateTime.AddDays(7),
                TimeToLive.Hour => item.StoredDateTime.AddHours(1),
            };
            DateTime atTheLatest = GetAtTheLatest(item.TimeToLive);
            if (DateTime.Now > atTheLatest)
            {
                static string ToString(TimeToLive ttl) => ttl switch
                {
                    TimeToLive.Week => "week",
                    TimeToLive.Day => "day",
                    TimeToLive.Hour => "hour",
                };
                var ttlString = ToString(item.TimeToLive);
                _logger.Log(LogLevel.Warning, $@"Tried to retrieve password for key [{key}] after date is expired. Key set at [{item}] for 1 [{ttlString}]");
                _items.Remove(key); // ensure "read-once" is implemented
                return null;
            }
            _items.Remove(key); // ensure "read-once" is implemented
            return item.EncryptedPassword;
        }
    }
}
