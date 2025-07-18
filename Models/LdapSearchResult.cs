namespace GM4ManagerWPF.Models
{
    public class LdapSearchResult
    {
        public string DistinguishedName { get; set; } = string.Empty;
        public string CommonName { get; set; } = string.Empty;
        public string SamAccountName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ObjectClass { get; set; } = ""; // z. B. "user" oder "group"
        public string Description { get; set; } = string.Empty;
        public override string ToString()
        {
            string desc= string.IsNullOrWhiteSpace(Description) ? "" : $"({Description})";
            string name = string.IsNullOrWhiteSpace(DisplayName) ? $"{SamAccountName}" : $"{DisplayName}";
            return $"{name} {desc}";
        }
    }
}
