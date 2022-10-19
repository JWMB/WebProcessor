using Organization.Repositories;
using Organization.Roles;

namespace Organization.Entities
{
    // init vs required discussion https://github.com/dotnet/csharplang/issues/3630
    public record School : IDocumentId, IWithSerializable<School.Serialized>
    {
        public School()
        {
            Classes = new ClassCollection(this);
        }
        public string Name { get; set; } = string.Empty;
        public ClassCollection Classes { get; }

        public IEnumerable<ClassTeacher> Teachers => Classes.SelectMany(o => o.Teachers).OfType<ClassTeacher>();
        public IEnumerable<ClassStudent> Students => Classes.SelectMany(o => o.Students).OfType<ClassStudent>();

        public Guid Id { get; internal set; } = Guid.NewGuid();

        // How to serialize? Id needed for navigation of course. Since 
        // First load all instances, then walk through all navigations
        // Students.ForEach(o => o.Class = idToClass[o.ClassId])
        // 

        public class ClassCollection : ReferenceAddingCollectionFromDefinition<School, Class, ClassDef>
        {
            public ClassCollection(School parent, IEnumerable<ClassDef>? children = null)
                : base(parent, (def, parent) => { var child = def.Instantiate(parent); return child; }, children)
            {
            }
        }

        public Serialized ToSerializable() => Serialized.FromInstance(this);

        public class Serialized : IDocumentId, IToBusinessObject<School>
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public List<Guid> Classes { get; set; } = new();
            public static Serialized FromInstance(School value)
            {
                return new Serialized
                {
                    Id = value.Id,
                    Name = value.Name,
                    Classes = value.Classes.Select(o => o.Id).ToList(),
                };
            }

            public School ToBusinessObject(IRepositoryService repos)
            {
                return new School { Id = Id, Name = Name };
            }
        }
    }
}
