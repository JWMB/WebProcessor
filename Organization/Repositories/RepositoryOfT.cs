namespace Organization.Repositories
{
    public class RepositoryOfT<T> : IRepositoryWithIdOfT<T> where T : IDocumentId
    {
        protected readonly IKeyValueStore<string, Document> keyValueStore;

        public RepositoryOfT(IKeyValueStore<string, Document> keyValueStore)
        {
            this.keyValueStore = keyValueStore;
        }

        protected virtual string TypeName => typeof(T).FullName ?? typeof(T).Name;
        public virtual void Add(T item)
        {
            keyValueStore.Add(item.Id.ToString(), new Document(item.Id.ToString(), TypeName, Serialize(item)));
        }

        public void Remove(T item) => Remove(item.Id);

        public void Remove(Guid id) => keyValueStore.Delete(id.ToString());

        public T? Get(Guid id)
        {
            var item = keyValueStore.Get(id.ToString());
            return item == null || item.Type != TypeName ? default(T) : Deserialize(item.Data);
        }

        public IEnumerable<T> Get(IEnumerable<Guid> ids)
        {
            var stringIds = ids.Select(o => o.ToString());
            var docs = keyValueStore.GetValues().Where(o => stringIds.Contains(o.Key)).Select(o => o.Value);
            return Deserialize(docs);
        }

        public IEnumerable<T> Query()
        {
            return Deserialize(keyValueStore.GetValues().Select(o => o.Value).Where(o => o.Type == TypeName));
        }

        private IEnumerable<T> Deserialize(IEnumerable<Document> kvs)
        {
            return kvs.Where(o => o.Type == TypeName)
                .Select(o => Deserialize(o.Data))
                .Where(o => o != null).Cast<T>();
        }

        public void Update(T item)
        {
            keyValueStore.Update(item.Id.ToString(), new Document(item.Id.ToString(), TypeName, Serialize(item)));
        }

        public virtual T? Deserialize(string? value)
        {
            if (value == null)
                return default(T);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(value);
        }

        public virtual string Serialize(T value)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(value);
        }
    }
}
