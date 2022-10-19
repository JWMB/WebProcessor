using Organization.Entities;
using Organization.Roles;

namespace Organization.Repositories
{
    public interface IRepositoryService
    {
        BusinessClassRepositoryOfT<School, School.Serialized> Schools { get; }
        BusinessClassRepositoryOfT<Class, Class.Serialized> Classes { get; }
        BusinessClassRepositoryOfT<ClassStudent, ClassStudent.Serialized> Students { get; }
    }

    public class RepositoryService : IRepositoryService
    {
        private readonly IKeyValueStore<string, Document> store;

        //public SchoolRepository Schools { get; private set; }
        //public ClassRepository Classes { get; private set; }
        public BusinessClassRepositoryOfT<School, School.Serialized> Schools { get; private set; }
        public BusinessClassRepositoryOfT<Class, Class.Serialized> Classes { get; private set; }
        public BusinessClassRepositoryOfT<ClassStudent, ClassStudent.Serialized> Students { get; private set; }

        public RepositoryService(IKeyValueStore<string, Document> store)
        {
            this.store = store;

            //Schools = new SchoolRepository(new RepositoryOfT<School.Serialized>(store));
            //Classes = new ClassRepository(new RepositoryOfT<Class.Serialized>(store));
            Schools = new BusinessClassRepositoryOfT<School, School.Serialized>(new RepositoryOfT<School.Serialized>(store));
            Classes = new BusinessClassRepositoryOfT<Class, Class.Serialized>(new RepositoryOfT<Class.Serialized>(store));
            Students = new BusinessClassRepositoryOfT<ClassStudent, ClassStudent.Serialized>(new RepositoryOfT<ClassStudent.Serialized>(store)); //ClassStudentRepository(new RepositoryOfT<ClassStudent.Serialized>(store));
        }

        public void Init()
        {
            Refresh(store.GetValues().Select(o => Guid.Parse(o.Key)));
        }

        public void Refresh(IEnumerable<Guid> ids)
        {
            //var docs = store.GetValues(ids.Select(o => o.ToString())).Select(o => o.Value).ToList();
            Schools.StoreRefresh(ids, this);

            Classes.StoreRefresh(ids, this);
            Students.StoreRefresh(ids, this);
        }
    }
}
