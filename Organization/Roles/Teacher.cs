using Organization.Entities;

namespace Organization.Roles
{
    public class ClassTeacherDef
    {
        public ClassTeacherDef(Person person)
        {
            Person = person;
        }

        public Person Person { get; }

        public ClassTeacher Instantiate(Class @class)
        {
            var item = new ClassTeacher(@class);
            Person.Roles.Add(item);
            //Assignments?.ToList().ForEach(o => item.Assignments.Add(o));
            return item;
        }
    }

    public class ClassTeacher : PersonRole
    {
        internal ClassTeacher(Class @class)
        {
            Class = @class;
        }

        //// https://github.com/dotnet/csharplang/issues/3630

        public Class Class { get; init; }

        public override IEnumerable<QualifiedRoleConnection> ConnectedRoles()
        {
            foreach (var item in Class.Students.Select(s => new QualifiedRoleConnection(s, RoleQualifier.Write)))
                yield return item;
        }
    }

    // It should not be possible to set just one "part" of a relationship, e.g. Class -> School without simultaneously setting School -> Class
    // How to model?
    // var class = new School().Classes.Add(new ClassDef()) - fine
    // Should these relationships always be immutable? (A class cannot change school) But say a school splits into two? Makes sense that all students are xferd to new classes
    // But when we create/add a role to a person (e.g. ClassStudent), how to then also add it to a Class?
    // Needs to be done at same time... new ClassStudentDef(Person)

    //public class Teacher : PersonRole
    //{
    //    // TODO: should it be ClassTeacher instead - one class per PersonRole? Keeps it simpler
    //    public Teacher(School school)
    //    {
    //        School = school;
    //        Classes = new ClassCollection(this);
    //    }
    //    public School School { get; init; }
    //    public ClassCollection Classes { get; }
    //    public override IEnumerable<QualifiedRoleConnection> ConnectedRoles()
    //    {
    //        foreach (var item in Classes.SelectMany(o => o.Students.Select(s => new QualifiedRoleConnection(s, RoleQualifier.Write))))
    //            yield return item;
    //        foreach (var item in School.Classes.Except(Classes).SelectMany(o => o.Students.Select(s => new QualifiedRoleConnection(s, RoleQualifier.None))))
    //            yield return item;
    //    }
    //    public class ClassCollection : ReferenceAddingCollection<Teacher, Class>
    //    {
    //        public ClassCollection(Teacher parent, IEnumerable<Class>? children = null)
    //            : base(parent, (c, p) => c.Teachers.Add(p))
    //        {
    //            children?.ToList().ForEach(Add);
    //        }
    //    }
    //}
}
