using System.Collections.Concurrent;

namespace Organization.Repositories
{
    public class BusinessClassRepositoryOfT<TBusinessClass, TSerializable> : IRepository<TBusinessClass>
        where TBusinessClass : IDocumentId, IWithSerializable<TSerializable>
        where TSerializable : IDocumentId, IToBusinessObject<TBusinessClass>
    {
        protected readonly RepositoryOfT<TSerializable> serializedRepository;
        protected readonly ConcurrentDictionary<Guid, TBusinessClass> cache = new();

        public BusinessClassRepositoryOfT(RepositoryOfT<TSerializable> serializedRepository)
        {
            this.serializedRepository = serializedRepository;
        }

        private TSerializable ToSerializable(TBusinessClass item) => item.ToSerializable();

        public void Add(TBusinessClass item)
        {
            cache.AddOrUpdate(item.Id, item, (k, old) => item);
            serializedRepository.Add(ToSerializable(item));
        }
        public void Update(TBusinessClass item)
        {
            //TODO: cache.TryUpdate(item.Id, item)
            cache.AddOrUpdate(item.Id, item, (k, old) => item);
            serializedRepository.Update(ToSerializable(item));
        }

        public TBusinessClass Get(Guid id) => cache[id];

        public IEnumerable<TSerializable> StoreRefresh(IEnumerable<Guid> ids, IRepositoryService repos)
        {
            var serialized = serializedRepository.Get(ids);
            var deleted = ids.Except(serialized.Select(o => o.Id)).ToList();
            deleted.ForEach(o => cache.TryRemove(o, out _));

            // TODO: we must check the deleted entries - e.g. if a Class was removed, we must remove ClassTeacher, reference in School, Students etc...
            // Express these relationships in a clearer way so they can be handled centrally?
            //deleted.ForEach(o => o.School.Classes.Remove(o));

            foreach (var item in serialized)
            {
                var bo = item.ToBusinessObject(repos);
                cache.AddOrUpdate(bo.Id, bo, (k, old) => bo);
            }


            return serialized;
        }

        public void Remove(TBusinessClass item)
        {
            cache.TryRemove(item.Id, out _);
            serializedRepository.Remove(item.Id);
        }

        public IEnumerable<TBusinessClass> Query() => cache.Values;
    }
}
