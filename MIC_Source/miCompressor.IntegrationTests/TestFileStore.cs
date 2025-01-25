using miCompressor.core;
using System.Dynamic;

namespace miCompressor.IntegrationTests;

public class TestFileStore
{

    private dynamic testDir = new ExpandoObject();

    public dynamic GetBasicDir()
    {
        testDir.files = new string[] { "x\\abc.jpg", "x/y/2.jpg", "sdfs/ss.png" };
        testDir.totalfiles_with_includeSubDir = 3;
        testDir.totalfiles_without_includeSubDir = 0;
        return testDir;
    }

    [Fact]
    public async Task TestBasicOpsSinglePath()
    {
        // Arrange
        dynamic testDirMeta = GetBasicDir();
        string testName = "FileStructureTest";
        var fileList = testDirMeta.files;


        // Act
        string testDir = TestFileHelper.CreateDirStructure(testName, fileList);

        FileStore store = new FileStore();

        //Add
        /*store.Add(testDir);

        Assert.True(store.SelectedPaths.Count == 1, $"Failed: Expected 1 selected path, actual {store.SelectedPaths.Count} after adding dir without sub-dir");
        Assert.True(store.GetAllFiles.Count == 0, $"Failed: Expected 1 media info, actual {store.GetAllFiles.Count} after adding dir without sub-dir ");

        store.RemoveAll();

        Assert.True(store.SelectedPaths.Count == 0, "Failed: Not removing all selected path after RemoveAll");
        Assert.True(store.GetAllFiles.Count == 0, "Failed: Not removing all selected path after RemoveAll");*/

        store.Add(testDir, true);
        await TestFileHelper.WaitUntil(() => store.GetAllFiles.Count > 0, TimeSpan.FromSeconds(5));

        Assert.True(store.SelectedPaths.Count == 1, $"Failed: Expected 1 selected path, actual {store.SelectedPaths.Count} after adding dir with sub-dir");        
        Assert.True(store.GetAllFiles.Count == 3, $"Failed: Expected 3 media info, actual {store.GetAllFiles.Count} after adding dir with sub-dir ");

        store.Remove(testDir);

        Assert.True(store.SelectedPaths.Count == 0, "Failed: Not removing all selected path after Remove by path");
        Assert.True(store.GetAllFiles.Count == 0, "Failed: Not removing all selected path after Remove by path");

        // Cleanup
        //Directory.Delete(testDir, true);
    }
}
