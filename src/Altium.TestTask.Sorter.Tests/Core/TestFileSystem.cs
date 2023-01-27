using System.IO.Abstractions.TestingHelpers;
using Altium.TestTask.Sorter.Abstractions;

namespace Altium.TestTask.Sorter.Tests.Core;

internal class TestFileSystem : MockFileSystem, IFileSystem
{
    public TestFileSystem(string tempDir)
        : base(null)
    {
        TempDir = tempDir;
    }

    public string GetFullPath(string fileName) => this.Path.Combine(TempDir, fileName);

    public string TempDir { get; }

    public bool FileExistsInTempDir(string fileName) => this.File.Exists(this.Path.Combine(TempDir, fileName));
}