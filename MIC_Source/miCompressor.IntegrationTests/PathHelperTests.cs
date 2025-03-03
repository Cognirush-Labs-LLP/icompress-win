using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using miCompressor.core;

namespace miCompressor.IntegrationTests;

public class PathHelperTests
{

    [Fact]
    public async Task TestGetCommonParentForEmptyList()
    {
        List<string> input = new() { };
        var output = PathHelper.GetCommonParent(input);
        Assert.Equal(null, output);
    }

    [Fact]
    public async Task TestGetCommonParentForSingleFileWithNoRootFolder()
    {
        List<string> input = new() { 
            @"C:\abc.ext"
        };
        var output = PathHelper.GetCommonParent(input);
        Assert.Equal(null, output);
    }

    [Fact]
    public async Task TestGetCommonParentForSingleFileWithRootFolder()
    {
        List<string> input = new() {
            @"C:\root\abc.ext"
        };
        var output = PathHelper.GetCommonParent(input);
        Assert.Equal(@"C:\", output);
    }

    [Fact]
    public async Task TestGetCommonParentForSingleFolderWithNoRootFolder()
    {
        List<string> input = new() {
            @"C:\root"
        };
        var output = PathHelper.GetCommonParent(input);
        Assert.Equal(@"C:\", output);
    }

    [Fact]
    public async Task TestGetCommonParentForSingleFolderWithRootFolder()
    {
        List<string> input = new() {
            @"C:\parent\abc"
        };
        var output = PathHelper.GetCommonParent(input);
        Assert.Equal(@"C:\parent", output);
    }

    [Fact]
    public async Task TestGetCommonParentForFilesOnDifferentDrive()
    {
        List<string> input = new() {
            @"C:\abc\parent\abc.txt",
            @"D:\abc\parent\def.txt"
        };
        var output = PathHelper.GetCommonParent(input);
        Assert.Equal(null, output);
    }

    [Fact]
    public async Task TestGetCommonParentForFilesOnSameDriveSameParent()
    {
        List<string> input = new() {
            @"C:\abc\parent\abc.txt",
            @"C:\abc\parent\def.txt"
        };
        var output = PathHelper.GetCommonParent(input);
        Assert.Equal(@"C:\abc", output);
    }

    [Fact]
    public async Task TestGetCommonParentForFilesAndFoldersDifferentParentInSameFolder1()
    {
        List<string> input = new() {
            @"C:\abc\parent1\abc.txt",
            @"C:\abc\parent2\def.txt"
        };
        var output = PathHelper.GetCommonParent(input);
        Assert.Equal(@"C:\abc", output);
    }

    [Fact]
    public async Task TestGetCommonParentForFilesAndFoldersDifferentParentInSameFolder2()
    {
        List<string> input = new() {
            @"C:\level\abc\abc.txt",
            @"C:\level\abc\def"
        };
        var output = PathHelper.GetCommonParent(input);
        Assert.Equal(@"C:\level", output);
    }

    [Fact]
    public async Task TestGetCommonParentForFilesAndFoldersDifferentParentWithComplexStructure1()
    {
        List<string> input = new() {
            @"C:\level1\level2\abc\abc.txt",
            @"C:\level1\level2\def\abc.txt",
            @"C:\level1\level2\dd",
            @"C:\level1\level2\abc\abc.txt",
            @"C:\level1\level2\abc.txt"
        };
        var output = PathHelper.GetCommonParent(input);
        Assert.Equal(@"C:\level1", output);
    }

    [Fact]
    public async Task TestGetCommonParentForFilesAndFoldersDifferentParentWithComplexStructure2()
    {
        List<string> input = new() {
            @"C:\level1\level2\abc\abc.txt",
            @"C:\level1\level2\def\abc.txt",
            @"C:\level1\level2\level3\",
            @"C:\level1\level2\abc\abc.txt",
            @"C:\level1\level2\abc"
        };
        var output = PathHelper.GetCommonParent(input);
        Assert.Equal(@"C:\level1\level2", output);
    }


}
