using Organization.Entities;

namespace Organization
{
    public abstract class PersonRole : IDocumentId
    {
        public Person Person { get; private set; }

        public Guid Id { get; internal set; } = Guid.NewGuid();

        internal void SetPerson(Person person) => Person = person;

        public abstract IEnumerable<QualifiedRoleConnection> ConnectedRoles();
    }

    public enum RoleQualifier
    {
        None = 0,
        Read = 1,
        Write = 2
    }

    public record QualifiedRoleConnection(PersonRole Role, RoleQualifier Qualifier);

    public interface IHasOwnAssignments
    {
        IEnumerable<Assignment> AssignmentsCollection { get; }
    }

    public class AssignmentCollection<T> : ReferenceAddingCollection<T, Assignment>
        where T : PersonRole
    {
        public AssignmentCollection(T parent, IEnumerable<Assignment>? children = null)
            : base(parent, (c, p) => c.Role = p)
        {
            children?.ToList().ForEach(Add);
        }
    }
}
