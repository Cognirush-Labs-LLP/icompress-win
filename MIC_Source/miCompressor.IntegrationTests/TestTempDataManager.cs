using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using miCompressor.core;
using miCompressor.IntegrationTests;

namespace miCompressor.Tests;

    public class TempDataManagerTests : IDisposable
{
    private readonly string _tempAppDir;

    public TempDataManagerTests()
    {
        _tempAppDir = TempDataManager.tempAppDir;
        Directory.CreateDirectory(_tempAppDir);  // Ensure the temp directory exists
    }

    [Fact]
    public async Task CleanUpTempDir_ShouldDeleteFilesOlderThan2Hours()
    {
        // Arrange
        string oldPreviewFile = TempDataManager.getTempPreviewFilePath("oldPreviewFile.txt");
        string recentPreviewFile = TempDataManager.getTempPreviewFilePath("recentPreviewFile.txt");
        string oldCacheFile = TempDataManager.GetTempStorageFilePath("subdir", "oldCacheFile.txt");
        string recentCacheFile = TempDataManager.GetTempStorageFilePath("subdir", "recentCacheFile.txt");

        // Create test files with different modification times
        CreateTestFile(oldPreviewFile, DateTime.Now.AddHours(-3));   // Should be deleted
        CreateTestFile(recentPreviewFile, DateTime.Now.AddMinutes(-30)); // Should NOT be deleted
        CreateTestFile(oldCacheFile, DateTime.Now.AddHours(-5));     // Should be deleted
        CreateTestFile(recentCacheFile, DateTime.Now.AddMinutes(-15));  // Should NOT be deleted

        // Act
        TempDataManager.CleanUpTempDir();
        await TestFileHelper.WaitUntil(() => !File.Exists(oldPreviewFile), TimeSpan.FromSeconds(5));
        await TestFileHelper.WaitUntil(() => !File.Exists(oldCacheFile), TimeSpan.FromSeconds(5));
        await Task.Delay(200);  // Wait for the async task to complete

        // Assert
        Assert.False(File.Exists(oldPreviewFile), "oldPreviewFile.txt should have been deleted.");
        Assert.True(File.Exists(recentPreviewFile), "recentPreviewFile.txt should NOT have been deleted.");
        Assert.False(File.Exists(oldCacheFile), "oldCacheFile.txt should have been deleted.");
        Assert.True(File.Exists(recentCacheFile), "recentCacheFile.txt should NOT have been deleted.");
    }

    [Fact]
    public async Task CleanUpTempDir_ShouldDeleteEmptyDirectories()
    {
        // Arrange
        string nestedDir = Path.Combine(TempDataManager.tempAppDir, "preview", "nestedDir");
        Directory.CreateDirectory(nestedDir);
        string oldNestedFile = Path.Combine(nestedDir, "oldNestedFile.txt");
        CreateTestFile(oldNestedFile, DateTime.Now.AddHours(-3));  // Should be deleted

        // Act
        // Wait until the old file is deleted or timeout
        TempDataManager.CleanUpTempDir();
        await TestFileHelper.WaitUntil(() => !Directory.Exists(nestedDir), TimeSpan.FromSeconds(5));


        // Assert
        Assert.False(Directory.Exists(nestedDir), "nestedDir should have been deleted.");
    }



    private void CreateTestFile(string filePath, DateTime lastWriteTime)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);  // Ensure the directory exists
        File.WriteAllText(filePath, "Test content.");                // Create the file
        File.SetLastWriteTime(filePath, lastWriteTime);              // Set its last write time
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempAppDir))
        {
            Directory.Delete(_tempAppDir, true);
        }
    }
}