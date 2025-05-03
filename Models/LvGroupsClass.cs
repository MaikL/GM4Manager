namespace GM4ManagerWPF
{
    public class LvGroupsClass
    {
        public required string Cn { get; set; }
        public required string DistinguishedName { get; set; }
        public string? Description { get; set; }
        public List<string>? Members { get; set; }
    }
}
