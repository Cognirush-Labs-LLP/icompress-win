using System;
using System.IO;
using Xunit;
using miCompressor.core;
using miCompressor.IntegrationTests;

namespace miCompressor.Tests.MediaFileInfoTests
{
    public class GetOutputPathTests 
    {
        private readonly string _tempDirectory;

        public GetOutputPathTests()
        {
            // Setup: Create a temporary directory for test purposes
            _tempDirectory = Path.Combine(Path.GetTempPath(), "GetOutputPathTests");
            Directory.CreateDirectory(_tempDirectory);
        }

        public void Dispose()
        {
            // Teardown: Clean up temporary directory after each test
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        [Fact]
        public void PreviewModeWithReplaceOriginal_ReturnsTempPath()
        {
            // Arrange
            var outputSettings = CreateOutputSettings(OutputLocationSettings.ReplaceOriginal);
            bool onlyPreview = true;
            var mediaFile1 = CreateMediaFileInfo("abc\\def\\test.jpg");
            var mediaFile2 = CreateMediaFileInfoAsSelectedPath("abc\\def\\test.jpg");

            // Act
            string result1 = mediaFile1.GetOutputPath(outputSettings, onlyPreview);
            string result2 = mediaFile2.GetOutputPath(outputSettings, onlyPreview);

            // Assert
            Assert.True(mediaFile1.IsReplaceOperation);
            Assert.True(TestFileHelper.IsSamePath(result1, Path.Combine(TempDataManager.GetTempStorageDirPath("abc\\def") , "test.jpg")));
            
            Assert.True(mediaFile2.IsReplaceOperation);
            Assert.True(TestFileHelper.IsSamePath(result2, Path.Combine(TempDataManager.GetTempStorageDirPath(""), "test.jpg")));

        }

        [Fact]
        public void OutputInCompressedFolder_ReturnsCorrectPath()
        {
            // Arrange
            var outputSettings = CreateOutputSettings(OutputLocationSettings.InCompressedFolder);
            var mediaFile1 = CreateMediaFileInfo("def\\ghq\\image.png");
            var mediaFile2 = CreateMediaFileInfoAsSelectedPath("def\\ghq\\image.png");

            var outputSettingsConvertToJPEG = CreateOutputSettings(OutputLocationSettings.InCompressedFolder);

            // Act
            string result1 = mediaFile1.GetOutputPath(outputSettings, false);
            string result2 = mediaFile2.GetOutputPath(outputSettings, false);
            string result3 = mediaFile1.GetOutputPath(outputSettingsConvertToJPEG, false);
            string result4 = mediaFile2.GetOutputPath(outputSettingsConvertToJPEG, false);


            // Assert
            string expectedPath1 = Path.Combine(_tempDirectory, "Compressed", "def\\ghq", "image.png");
            Assert.True(TestFileHelper.IsSamePath(result1, expectedPath1));
            
            string expectedPath2 = Path.Combine(_tempDirectory, "def\\ghq", "Compressed", "image.png");
            Assert.True(TestFileHelper.IsSamePath(result2, expectedPath2));

            string expectedPath3 = Path.Combine(_tempDirectory, "Compressed", "def\\ghq", "image.jpg");
            Assert.True(TestFileHelper.IsSamePath(result3, expectedPath3));

            string expectedPath4 = Path.Combine(_tempDirectory, "def\\ghq", "Compressed", "image.jpg");
            Assert.True(TestFileHelper.IsSamePath(result4, expectedPath4));
        }


        [Fact]
        public void UserSpecificFolderWithFolderSet_ReturnsCorrectPath()
        {
            // Arrange
            var outputSettings = CreateOutputSettings(OutputLocationSettings.UserSpecificFolder);
            outputSettings.outputFolder = "C:\\output folder\\";
            var mediaFile1 = CreateMediaFileInfo("c  d\\ghi\\fil e.bmp");
            var mediaFile2 = CreateMediaFileInfoAsSelectedPath("c  d\\ghi\\fil e.bmp");

            // Act
            string result1 = mediaFile1.GetOutputPath(outputSettings, false);
            string result2 = mediaFile2.GetOutputPath(outputSettings, false);


            // Act & Assert
            string expectedPath1 = "C:\\output folder\\c  d\\ghi\\fil e.bmp";
            string expectedPath2 = "C:\\output folder\\fil e.bmp";

            Assert.True(TestFileHelper.IsSamePath(result1, expectedPath1));
            Assert.True(TestFileHelper.IsSamePath(result2, expectedPath2));
        }

        [Fact]
        public void ReplaceFromAndReplaceToTransformation_AppliedCorrectly()
        {
            // Arrange
            var outputSettings = CreateOutputSettings(OutputLocationSettings.SameFolderWithFileNameSuffix);
            outputSettings.replaceFrom = "IMG_";
            outputSettings.replaceTo = "PHOTO_";
            var mediaFile1 = CreateMediaFileInfo("ReplaceFromAndReplaceToTransformation_AppliedCorrectlyIMG_OK/IMG_sample.jpg");

            // Act
            string result1 = mediaFile1.GetOutputPath(outputSettings, false);

            // Assert
            Assert.Equal(Path.Combine(_tempDirectory, "ReplaceFromAndReplaceToTransformation_AppliedCorrectlyIMG_OK\\PHOTO_sample.jpg"), result1);

            // Arange
            outputSettings.replaceFrom = "@3x";
            outputSettings.replaceTo = "@2x";
            var mediaFile2 = CreateMediaFileInfo("ReplaceFromAndReplaceToTransformation_AppliedCorrectly/IMG_sample@3x.jpg");

            // Act
            string result2 = mediaFile2.GetOutputPath(outputSettings, false);

            // Assert
            Assert.True(TestFileHelper.IsSamePath(Path.Combine(_tempDirectory, "ReplaceFromAndReplaceToTransformation_AppliedCorrectly/IMG_sample@2x.jpg"), result2));
        }

        [Fact]
        public void PrefixAndSuffixAppliedToFileName_ReturnsCorrectPath()
        {
            // Arrange
            var outputSettings = CreateOutputSettings(OutputLocationSettings.SameFolderWithFileNameSuffix);
            outputSettings.prefix = "PRE_";
            outputSettings.suffix = "_SUF";
            var mediaFile = CreateMediaFileInfo("test123.jpg");

            // Act
            string result = mediaFile.GetOutputPath(outputSettings, false);

            // Assert
            string expectedFileName = "PRE_test123_SUF.jpg";
            Assert.Contains(expectedFileName, result);
        }

        [Fact]
        public void UserSpecificFolderWithoutFolderSetThrowsInvalidOperationException()
        {
            // Arrange
            var outputSettings = CreateOutputSettings(OutputLocationSettings.UserSpecificFolder);
            outputSettings.outputFolder = string.Empty;
            var mediaFile = CreateMediaFileInfo("ghi\\file.bmp");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mediaFile.GetOutputPath(outputSettings, false));
        }

        [Fact]
        public void UnsupportedOutputLocationSettings_ThrowsNotSupportedException()
        {
            // Arrange
            var outputSettings = CreateOutputSettings((OutputLocationSettings)99); // Unsupported setting
            var mediaFile = CreateMediaFileInfo("random.jpg");

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => mediaFile.GetOutputPath(outputSettings, false));
        }

        private OutputSettings CreateOutputSettings(OutputLocationSettings locationSettings)
        {
            return new OutputSettings
            {
                format = OutputFormat.KeepSame,
                outputLocationSettings = locationSettings,
                outputFolder = _tempDirectory,
                prefix = string.Empty,
                suffix = string.Empty,
                replaceFrom = string.Empty,
                replaceTo = string.Empty
            };
        }

        private MediaFileInfo CreateMediaFileInfo(string fileName)
        {
            string filePath = Path.Combine(_tempDirectory, fileName);

            if (Directory.Exists(Path.GetDirectoryName(filePath)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }

            File.WriteAllText(filePath, "dummy content");
            return new MediaFileInfo(_tempDirectory, new FileInfo(filePath));
        }

        private MediaFileInfo CreateMediaFileInfoAsSelectedPath(string fileName)
        {
            string filePath = Path.Combine(_tempDirectory, fileName);

            if (Directory.Exists(Path.GetDirectoryName(filePath)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }

            File.WriteAllText(filePath, "dummy content");
            return new MediaFileInfo(filePath, new FileInfo(filePath));
        }
    }
}

