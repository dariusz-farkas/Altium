using System.IO.Abstractions.TestingHelpers;
using Altium.TestTask.Sorter.Configuration;
using Altium.TestTask.Sorter.Tests.Core;
using Microsoft.Extensions.Options;
using Moq;
using Moq.AutoMock;
using IFileSystem = Altium.TestTask.Sorter.Abstractions.IFileSystem;

namespace Altium.TestTask.Sorter.Tests.FileMergerTests;

[TestFixture]
public class MergeTests : UnitTestBase<MergeTestsContext>
{
    [Test]
    public async Task WhenMerged_ShouldCreateSingleSortedFile()
    {
        // arrange
        var fileNames = new List<string>();
        for (var i = 1; i <= 5; i++)
        {
            var fileName = await AddFileToWorkspace(i);
            fileNames.Add(fileName);
        }
        
        // act
        var rs = await this.Context.Sut.Merge(fileNames, CancellationToken.None);

        //assert
        Assert.IsNotNull(rs);
        Assert.That(this.Context.FileSystem.FileExists(rs!));

        var actualMergedText = await this.Context.FileSystem.File.ReadAllTextAsync(rs!);
        var expectedMergedText = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\result.txt"));

        Assert.That(actualMergedText, Is.EqualTo(expectedMergedText));
    }

    private async Task<string> AddFileToWorkspace(int i)
    {
        var fileName = $"sorted-{i}.txt";
        var text = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory,
            @$"TestData\{fileName}"));
        this.Context.FileSystem.AddFile(@$"C:\workspace\{fileName}", new MockFileData(text));
        return fileName;
    }
}

public class MergeTestsContext : IUnitTestContext
{
    internal FileMerger Sut { get; private set; } = null!;
    internal AutoMocker Mock = null!;
    internal TestFileSystem FileSystem { get; private set; } = null!;
    public void Setup()
    {
        Mock = new AutoMocker(MockBehavior.Loose);
        Mock.GetMock<IOptions<MergeOptions>>()
            .Setup(x => x.Value)
            .Returns(new MergeOptions()
            {
                ChunkSize = 3
            });

        FileSystem = new TestFileSystem("C:\\workspace");
        Mock.Use<IFileSystem>(FileSystem);

        Sut = Mock.CreateInstance<FileMerger>();
    }
}