using miCompressor.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using miCompressor.ui;

namespace miCompressor.IntegrationTests
{
    public class TestVersionChecker
    {
        int latestMajorVersion = 3;
        int latestMinorVersion = 3; //this shoudl match what's there at https://sourceforge.net/rest/p/icompress/wiki/Version
        //int? currentPatchVersion = 0;
        //int? currentBuildVersion = 0;

        string getCurrentVersionAs(int major, int minor)
        {
            return $"{Math.Max(0, major)}.{Math.Max(0,minor)}";
        }

        [Fact]
        public async Task TestWhenUpdateIsThere()
        {
            bool isVersionAvailable = await VersionChecker.CheckIfNewVersionAvailableAsync(getCurrentVersionAs(latestMajorVersion, latestMinorVersion - 1));
            Assert.True(isVersionAvailable);
            isVersionAvailable = await VersionChecker.CheckIfNewVersionAvailableAsync(getCurrentVersionAs(latestMajorVersion - 1, latestMinorVersion + 1));
            Assert.True(isVersionAvailable);
        }

        [Fact]
        public async Task TestWhenAlareadyLatest()
        {
            bool isVersionAvailable = await VersionChecker.CheckIfNewVersionAvailableAsync(getCurrentVersionAs(latestMajorVersion, latestMinorVersion));
            Assert.False(isVersionAvailable);
        }

        [Fact]
        public async Task TestWhenAlareadyHigherThanLatest()
        {
            bool isVersionAvailable = await VersionChecker.CheckIfNewVersionAvailableAsync(getCurrentVersionAs(latestMajorVersion, latestMinorVersion+1));
            Assert.False(isVersionAvailable);
            isVersionAvailable = await VersionChecker.CheckIfNewVersionAvailableAsync(getCurrentVersionAs(latestMajorVersion + 1 , latestMinorVersion + 1));
            Assert.False(isVersionAvailable);
            isVersionAvailable = await VersionChecker.CheckIfNewVersionAvailableAsync(getCurrentVersionAs(latestMajorVersion + 1, latestMinorVersion - 1));
            Assert.False(isVersionAvailable);
        }
    }
}
