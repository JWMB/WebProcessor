namespace Organization.Entities
{
    public class Person : IDocumentId
    {
        public Guid Id { get; internal set; } = Guid.NewGuid();

        public RoleCollection Roles { get; }
        public Person(IEnumerable<PersonRole>? roles = null)
        {
            Roles = new RoleCollection(this, roles);
        }

        public class RoleCollection : ReferenceAddingCollection<Person, PersonRole>
        {
            public RoleCollection(Person parent, IEnumerable<PersonRole>? children = null)
                : base(parent, (p, c) => p.SetPerson(c))
            {
                children?.ToList().ForEach(Add);
            }
        }
    }
}
