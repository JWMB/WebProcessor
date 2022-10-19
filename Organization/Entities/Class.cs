using Organization.Repositories;
using Organization.Roles;

namespace Organization.Entities
{
    public class ClassDef
    {
        public School? School { get; set; }

        public IList<ClassTeacherDef>? Teachers { get; set; }
        public IList<ClassStudentDef>? Students { get; set; }

        public string Name { get; set; } = "";

        public int Grade { get; set; }

        public Class Instantiate(School school)
        {
            var @class = new Class(school) { Name = Name, Grade = Grade };

            Teachers?.ToList().ForEach(o => @class.Teachers.Add(o));
            Students?.ToList().ForEach(o => @class.Students.Add(o));

            return @class;
        }
    }

    public class Class : IDocumentId, IWithSerializable<Class.Serialized>
    {
        internal Class(School school)
        {
            School = school;
            Students = new StudentCollection(this);
            Teachers = new TeacherCollection(this);
        }

        public School School { get; }

        public int Grade { get; set; }
        public string Name { get; set; } = string.Empty;
        public StudentCollection Students { get; }
        public TeacherCollection Teachers { get; }

        public Guid Id { get; internal set; } = Guid.NewGuid();

        public Serialized ToSerializable() => Serialized.FromInstance(this);

        public class TeacherCollection : ReferenceAddingCollectionFromDefinition<Class, ClassTeacher, ClassTeacherDef>
        {
            public TeacherCollection(Class parent, IEnumerable<ClassTeacherDef>? children = null)
                : base(parent, (def, parent) => { var child = def.Instantiate(parent); return child; }, children)
            {
            }
        }

        public class StudentCollection : ReferenceAddingCollectionFromDefinition<Class, ClassStudent, ClassStudentDef>
        {
            public StudentCollection(Class parent, IEnumerable<ClassStudentDef>? children = null)
                : base(parent, (def, parent) => { var child = def.Instantiate(parent); return child; }, children)
            {
            }
        }

        public class Serialized : IDocumentId, IToBusinessObject<Class>
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;

            public int Grade { get; set; }
            public Guid School { get; set; }

            public List<Guid> Students { get; set; } = new();
            public List<Guid> Teachers { get; set; } = new();

            public static Serialized FromInstance(Class value)
            {
                return new Serialized
                {
                    Id = value.Id,
                    Name = value.Name,
                    Grade = value.Grade,

                    School = value.School.Id,

                    Students = value.Students.Select(o => o.Id).ToList(),
                    Teachers = value.Teachers.Select(o => o.Id).ToList(),
                };
            }

            public Class ToBusinessObject(IRepositoryService repos)
            {
                var def = new ClassDef { Grade = Grade, Name = Name };
                var bo = repos.Schools.Get(School).Classes.Add(def);
                bo.Id = Id;
                return bo;
            }
        }
    }
}
