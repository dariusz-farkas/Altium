using Altium.TestTask.Sorter.Tests.Core;
using Altium.TestTask.Sorter.Utilities;

namespace Altium.TestTask.Sorter.Tests.Utilities;

[TestFixture]
public class FileSizeFormatterTests
{
    [Test]
    [TestCase(1024, "1,0 KB")]
    [TestCase(1024 * 1024, "1,0 MB")]
    [TestCase(1024 * 1024 * 1024, "1,0 GB")]
    [TestCase(1024 * 1024 * 1024 + 100 * 1024 * 1024, "1,1 GB")]
    [TestCase(100, "100,0 Bytes")]
    [TestCase(2000, "2,0 KB")]
    public void WhenBytesProvided_ShouldFormatSize(long bytes, string expected)
    {
        // act
        var result = FileSizeFormatter.FormatSize(bytes);

        //assert
        Assert.That(result, Is.EqualTo(expected));
    }
}