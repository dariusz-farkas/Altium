using System.Buffers;
using System.IO.Abstractions.TestingHelpers;
using Altium.TestTask.Sorter.Abstractions;
using Altium.TestTask.Sorter.Configuration;
using Altium.TestTask.Sorter.Tests.Core;
using Microsoft.Extensions.Options;
using Moq;
using Moq.AutoMock;

namespace Altium.TestTask.Sorter.Tests.FileSorterTests;

[TestFixture]
public class SortTests : UnitTestBase<SortTestsContext>
{
 
    [Test]
    public async Task WhenSorted_ShouldSortLinesInCorrectOrder()
    {
        // arrange
        const int lineCount = 16;
        var fileName = "input.txt";

        var text = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, @$"TestData\{fileName}"));
        this.Context.FileSystem.AddFile(@$"C:\workspace\{fileName}", new MockFileData(text));

        var fileData = new FileData(fileName, lineCount);

        // act
        await this.Context.Sut.Sort(fileData, ArrayPool<string>.Shared, CancellationToken.None);

        //assert
        Assert.That(this.Context.FileSystem.FileExistsInTempDir("input.txt"));
        var actualSortedText = await this.Context.FileSystem.File.ReadAllTextAsync(this.Context.FileSystem.GetFullPath(fileName));
        var expectedSortedText = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, @$"TestData\result.txt"));

        Assert.That(actualSortedText, Is.EqualTo(expectedSortedText));
    }
}

public class SortTestsContext : IUnitTestContext
{
    internal FileSorter Sut { get; private set; } = null!;
    internal AutoMocker Mock = null!;
    internal TestFileSystem FileSystem { get; private set; } = null!;
    public void Setup()
    {
        Mock = new AutoMocker(MockBehavior.Loose);
        Mock.GetMock<IOptions<SortOptions>>()
            .Setup(x => x.Value)
            .Returns(new SortOptions());

        FileSystem = new TestFileSystem("C:\\workspace");
        Mock.Use<IFileSystem>(FileSystem);

        Sut = Mock.CreateInstance<FileSorter>();
    }
}