using System.Collections.Concurrent;

namespace Organization.Repositories
{
    public interface IKeyValueStore<TKey, TValue>
    {
        IEnumerable<KeyValuePair<TKey, TValue>> GetValues();
        IEnumerable<KeyValuePair<TKey, TValue>> GetValues(IEnumerable<TKey> keys);
        void Update(TKey key, TValue value);
        void Add(TKey key, TValue value);
        void Delete(TKey key);
        TValue? Get(TKey v);
    }

    public record Document(string Key, string Type, string Data);

    public class DocumentStore : IKeyValueStore<string, Document>
    {
        private ConcurrentDictionary<string, Document> store = new ConcurrentDictionary<string, Document>();

        public void Delete(string key) => store.TryRemove(key, out _);
        public Document? Get(string key) => store.TryGetValue(key, out var value) ? value : null;
        public IEnumerable<KeyValuePair<string, Document>> GetValues(IEnumerable<string> keys) => store.Where(o => keys.Contains(o.Key));
        public IEnumerable<KeyValuePair<string, Document>> GetValues() => store;
        public void Update(string key, Document value)
        {
            store.AddOrUpdate(key, value, (k, old) => value);
        }

        public void Update(Document value) => Update(value.Key, value);

        public void Add(Document value)
        {
            store.AddOrUpdate(value.Key, value, (k, old) => value);
        }
        public void Add(string key, Document value) => Add(value);
    }
}
