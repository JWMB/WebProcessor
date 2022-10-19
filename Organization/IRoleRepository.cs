using Organization.Entities;
using Organization.Repositories;
using Organization.Roles;

namespace Organization
{
    public interface IDocumentId
    {
        public Guid Id { get; }
    }

    public interface IWithSerializable<T>
    {
        T ToSerializable();
    }

    public interface IToBusinessObject<T>
    {
        T ToBusinessObject(IRepositoryService repos);
    }

    public interface IRepository<T>
    {
        IEnumerable<T> Query();
        void Add(T item);
        void Update(T item);
        void Remove(T item);
    }

    public interface IRepositoryWithIdOfT<T> : IRepository<T>
        where T : IDocumentId
    {
        T? Get(Guid id);
        IEnumerable<T> Get(IEnumerable<Guid> ids);
        void Remove(Guid id);
    }

    //public class SchoolRepository : BusinessClassRepositoryOfT<School, School.Serialized>
    //{
    //    internal SchoolRepository(RepositoryOfT<School.Serialized> serializedRepository)
    //        : base(serializedRepository)
    //    { }
    //}

    //public class ClassRepository : BusinessClassRepositoryOfT<Class, Class.Serialized>
    //{
    //    internal ClassRepository(RepositoryOfT<Class.Serialized> serializedRepository)
    //        : base(serializedRepository)
    //    { }
    //}

    //public class ClassStudentRepository : BusinessClassRepositoryOfT<ClassStudent, ClassStudent.Serialized>
    //{
    //    internal ClassStudentRepository(RepositoryOfT<ClassStudent.Serialized> serializedRepository)
    //        : base(serializedRepository)
    //    { }
    //}
}
