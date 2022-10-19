using Organization.Roles;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace Organization.Entities
{
    public class Assignment
    {
        public PersonRole Role { get; set; }
        public string Id { get; set; }
    }
}
