using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GM4ManagerWPF.Classes
{
    public class LdapMember
    {
        public string? DistinguishedName { get; set; }
        public string? ObjectClass { get; set; } // "user" oder "group"
    }
}
