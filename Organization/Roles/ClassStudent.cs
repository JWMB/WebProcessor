using Organization.Entities;
using Organization.Repositories;

namespace Organization.Roles
{
    public class ClassStudentDef
    {
        public ClassStudentDef(Person person)
        {
            Person = person;
        }

        public Class? Class { get; set; }

        public IEnumerable<Assignment> Assignments { get; set; } = new List<Assignment>();
        public Person Person { get; }

        public ClassStudent Instantiate(Class @class)
        {
            var item = new ClassStudent(@class);
            Person.Roles.Add(item);
            Assignments?.ToList().ForEach(o => item.Assignments.Add(o));
            return item;
        }
    }

    public class ClassStudent : PersonRole, IHasOwnAssignments, IWithSerializable<ClassStudent.Serialized>
    {
        internal ClassStudent(Class @class)
        {
            Class = @class;
            Assignments = new AssignmentCollection<ClassStudent>(this);
        }

        public Class Class { get; set; }

        public AssignmentCollection<ClassStudent> Assignments { get; }
        public IEnumerable<Assignment> AssignmentsCollection => Assignments.Select(o => o);

        public override IEnumerable<QualifiedRoleConnection> ConnectedRoles()
        {
            return Enumerable.Empty<QualifiedRoleConnection>();
        }

        public Serialized ToSerializable() => Serialized.FromInstance(this);

        public class Serialized : IDocumentId, IToBusinessObject<ClassStudent>
        {
            public Guid Id { get; set; }
            public Guid Class { get; set; }
            public Guid Person { get; set; }

            public static Serialized FromInstance(ClassStudent value)
            {
                return new Serialized
                {
                    Id = value.Id,
                    Class = value.Class.Id,
                    Person = value.Person.Id,
                    //value.Assignments
                };
            }

            public ClassStudent ToBusinessObject(IRepositoryService repos) // Person person, Class @class)
            {
                var def = new ClassStudentDef(new Person()) { };
                var bo = repos.Classes.Get(Class).Students.Add(def);
                bo.Id = Id;
                return bo;
            }
        }
    }
}
