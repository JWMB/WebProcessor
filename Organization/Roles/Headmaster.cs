using Organization.Entities;

namespace Organization.Roles
{
    public class Headmaster : PersonRole
    {
        internal Headmaster(School school)
        {
            School = school;
        }

        public School School { get; }

        public override IEnumerable<QualifiedRoleConnection> ConnectedRoles()
        {
            foreach (var item in School.Teachers.Select(s => new QualifiedRoleConnection(s, RoleQualifier.Write)))
                yield return item;

            foreach (var item in School.Classes.SelectMany(o => o.Students.Select(s => new QualifiedRoleConnection(s, RoleQualifier.Write))))
                yield return item;
        }
    }
}
