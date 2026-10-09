using NUnit.Framework;
using Shouldly;
using SchoolClubs.Application.Services.Avatars;

namespace SchoolClubs.Application.UnitTests.Services;

[TestFixture]
[TestOf(typeof(AvatarImages))]
public class AvatarImagesTests
{
    [TestCase(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0 }, "image/png")]
    [TestCase(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "image/jpeg")]
    [TestCase(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, "image/gif")]
    [TestCase(new byte[] { 0x52, 0x49, 0x46, 0x46, 1, 2, 3, 4, 0x57, 0x45, 0x42, 0x50 }, "image/webp")]
    public void Detect_KnownSignature_ReturnsContentType(byte[] content, string contentType)
    {
        AvatarImages.Detect(content)?.ContentType.ShouldBe(contentType);
    }

    [TestCase(new byte[] { })]
    [TestCase(new byte[] { 0x3C, 0x73, 0x76, 0x67 })] // <svg
    [TestCase(new byte[] { 0x52, 0x49, 0x46, 0x46, 1, 2, 3, 4, 0x57, 0x41, 0x56, 0x45 })] // RIFF WAVE
    public void Detect_UnsupportedContent_ReturnsNull(byte[] content)
    {
        AvatarImages.Detect(content).ShouldBeNull();
    }
}
