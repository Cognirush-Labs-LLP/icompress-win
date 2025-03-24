using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace miCompressor.ui;

/// <summary>
/// This version checker checks version information from sourceforge. Microsoft Store users need not to worry about this.
/// </summary>
public class VersionChecker
{
    private const string VersionUrl = "https://sourceforge.net/rest/p/icompress/wiki/Version";

    /// <summary>
    /// Checks if a new version is available by comparing the current version (major.minor) with the version from the URL.
    /// </summary>
    /// <param name="currentVersion">The current version as a string (e.g., "4.3").</param>
    /// <returns>True if the fetched version's major or minor is greater than the current version; otherwise, false.</returns>
    public static async Task<bool> CheckIfNewVersionAvailableAsync(string currentVersion)
    {
        // Fetch the content from the URL.
        using HttpClient client = new HttpClient();
        string content = await client.GetStringAsync(VersionUrl);

        // Look for a version string like "Current Version is 3.3.2" or "Current Version is 4.0"
        var regex = new Regex(@"Current\s+Version\s+is\s+([\d\.]+)", RegexOptions.IgnoreCase);
        var match = regex.Match(content);
        if (!match.Success)
        {
            throw new Exception("Unable to find version information in the fetched content.");
        }

        string fetchedVersion = match.Groups[1].Value;

        // Parse the fetched version and current version to compare only major and minor parts.
        var fetchedParts = fetchedVersion.Split('.');
        var currentParts = currentVersion.Split('.');

        if (fetchedParts.Length < 2 || currentParts.Length < 2)
        {
            throw new Exception("Invalid version format. Expected at least major and minor version numbers.");
        }

        int fetchedMajor = int.Parse(fetchedParts[0]);
        int fetchedMinor = int.Parse(fetchedParts[1]);

        int currentMajor = int.Parse(currentParts[0]);
        int currentMinor = int.Parse(currentParts[1]);

        // Compare major version first, then minor.
        if (fetchedMajor > currentMajor)
        {
            return true;
        }
        else if (fetchedMajor == currentMajor && fetchedMinor > currentMinor)
        {
            return true;
        }

        return false;
    }
}
