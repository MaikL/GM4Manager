using GM4ManagerWPF.Classes;

namespace GM4ManagerWPF.Models
{
    public class LvGroupsClass
    {
        public required string Cn { get; set; }
        public required string DistinguishedName { get; set; }
        public string? Description { get; set; }
        public List<LdapSearchResult> Members { get; set; } = [];
        public bool IsPlaceholder = false;
    }
}
