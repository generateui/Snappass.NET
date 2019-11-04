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
                return null;
            }
            if (!_items.ContainsKey(key))
            {
                return null;
            }
            var item = _items[key];
            DateTime calculatedDate = item.StoredDateTime;
            switch(item.TimeToLive)
            {
                case TimeToLive.Day: calculatedDate.AddDays(1); break;
                case TimeToLive.Week: calculatedDate.AddDays(7); break;
                case TimeToLive.Hour: calculatedDate.AddHours(1); break;
            }
            if (calculatedDate > DateTime.Now)
            {
                return null;
            }
            _items.Remove(key); // ensure "read-once" is implemented
            return item.EncryptedPassword;
        }
    }
}
