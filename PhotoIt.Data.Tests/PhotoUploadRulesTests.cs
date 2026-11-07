using PhotoIt.Data;

namespace PhotoIt.Data.Tests;

public sealed class PhotoUploadRulesTests
{
    [Theory]
    [InlineData("photo.jpg", "image/jpeg", ".jpg")]
    [InlineData("photo.RW2", "application/octet-stream", ".rw2")]
    [InlineData("video.mp4", "video/mp4", ".mp4")]
    public void ValidateFile_AcceptsSupportedFormats(string fileName, string contentType, string expectedExtension)
    {
        var extension = PhotoUploadRules.ValidateFile(fileName, contentType, 1024);

        Assert.Equal(expectedExtension, extension);
        Assert.NotEmpty(PhotoUploadRules.GetContentType(extension));
    }

    [Theory]
    [InlineData("document.pdf", "application/pdf", 1024)]
    [InlineData("photo.jpg", "video/mp4", 1024)]
    [InlineData("video.mp4", "image/jpeg", 1024)]
    [InlineData("photo.jpg", "image/jpeg", 0)]
    [InlineData("photo.jpg", "image/jpeg", PhotoUploadRules.MaxFileLength + 1)]
    public void ValidateFile_RejectsUnsupportedTypeOrSize(string fileName, string contentType, long length)
    {
        Assert.Throws<InvalidOperationException>(
            () => PhotoUploadRules.ValidateFile(fileName, contentType, length));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(50, true)]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public void IsValidBatchFileCount_EnforcesBatchLimit(int count, bool expected)
    {
        Assert.Equal(expected, PhotoUploadRules.IsValidBatchFileCount(count));
    }
}
