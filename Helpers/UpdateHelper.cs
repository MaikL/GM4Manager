using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace GM4ManagerWPF.Helpers
{
    public class UpdateHelper
    {
        public bool IsCurrent { get; private set; } = true;
        public string? LatestVersionUrl { get; private set; }
        public string? LatestVersion { get; private set; }

        public async Task CheckForUpdateAsync(string currentVersion)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("request");

                var response = await client.GetStringAsync("https://api.github.com/repos/MaikL/GM4Manager/releases/latest");
                using var doc = JsonDocument.Parse(response);
                LatestVersion = doc.RootElement.GetProperty("tag_name").GetString() ?? string.Empty;

                if (IsNewerVersion(LatestVersion, currentVersion))
                {
                    LatestVersionUrl = doc.RootElement.GetProperty("html_url").GetString();
                    IsCurrent = false;
                }
                else
                {
                    IsCurrent = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex.Message}");
                IsCurrent = true;
            }
        }

        private bool IsNewerVersion(string latest, string current)
        {
            Version latestV = new (latest.TrimStart('v'));
            Version currentV = new (current);
            return latestV > currentV;
        }
    }

}