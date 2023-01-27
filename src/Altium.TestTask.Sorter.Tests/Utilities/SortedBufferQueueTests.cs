using Altium.TestTask.Sorter.Tests.Core;
using Altium.TestTask.Sorter.Utilities;
using static Altium.TestTask.Sorter.Tests.Utilities.SortedBufferQueueTestsContext;

namespace Altium.TestTask.Sorter.Tests.Utilities;

[TestFixture]
public class SortedBufferQueueTests : UnitTestBase<SortedBufferQueueTestsContext>
{
    [Test]
    public void WhenItemsAdded_ShouldPreserveOrdering()
    {
        // arrange
        var testData = new List<TestEntry>
        {
            new TestEntry("45. Apple", "1", 3),
            new TestEntry("304. Something", "2", 6),
            new TestEntry("1. Apple", "3", 2),
            new TestEntry("2. Banana is yellow", "4", 4),
            new TestEntry("2. Banana is yellow", "5", 5),
            new TestEntry("99. Aaaaa", "6", 1),
        };

        foreach (var testEntry in testData)
        {
            Context.Sut.Add(testEntry.Key, testEntry.Origin);
        }

        var expected = testData.OrderBy(x => x.ExpectedPosition).Select(x => (x.Key, x.Origin)).ToArray();

        // act & assert
        Assert.That(this.Context.Sut.Count, Is.EqualTo(testData.Count));

        for (var i = 0; i < testData.Count; i++)
        {
            var result = this.Context.Sut.TryDequeue(out var key, out var origin);
            Assert.True(result);

            Assert.That(key, Is.EqualTo(expected[i].Key));
            Assert.That(origin, Is.EqualTo(expected[i].Origin));
        }

        var hasElement = this.Context.Sut.TryDequeue(out _, out _);
        Assert.IsFalse(hasElement);
    }

    [Test]
    public void WhenAddedWithDuplicatedOrigin_ShouldThrowException()
    {
        // arrange
        Context.Sut.Add("1. abc", "origin");
        
        // act & assert
        TestDelegate action = () => Context.Sut.Add("1. abc", "origin");

        Assert.Throws<InvalidOperationException>(action);
    }
}

public class SortedBufferQueueTestsContext : IUnitTestContext
{
    internal SortedBufferQueue<string, string> Sut { get; private set; } = null!;
    public void Setup()
    {
        Sut = new SortedBufferQueue<string, string>(new CustomComparer());
    }

    internal sealed record TestEntry(string Key, string Origin, int ExpectedPosition);
}