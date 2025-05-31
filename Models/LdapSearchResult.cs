namespace GM4ManagerWPF.Models
{
    public class LdapSearchResult
    {
        public string DistinguishedName { get; set; } = string.Empty;
        public string CommonName { get; set; } = string.Empty;
        public string SamAccountName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ObjectClass { get; set; } = ""; // z. B. "user" oder "group"
        public override string ToString()
        {
            return $"{DisplayName} ({CommonName})";
        }
    }
}
