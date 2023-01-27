using Altium.TestTask.Sorter.Tests.Core;
using Altium.TestTask.Sorter.Utilities;

namespace Altium.TestTask.Sorter.Tests.Utilities;

[TestFixture]
public class AltiumComparerTests : UnitTestBase<AltiumComparerTestsContext>
{
    [Test]
    [TestCase("1. abc", "1. abc", 0)]
    [TestCase("1. abc", "2. abc", -1)]
    [TestCase("10. abc", "1. abc", 1)]
    [TestCase("1. abc", "2. abd", -1)]
    [TestCase("12. abc", "1. aad", 1)]
    [TestCase("2. ab", "99. a", 1)]
    public void WhenCompared_ShouldReturnCorrectResult(string left, string right, int expectedResult)
    {
        // act
        var result = this.Context.Sut.Compare(left, right);

        //assert
        Assert.That(result, Is.EqualTo(expectedResult));
    }
}

public class AltiumComparerTestsContext : IUnitTestContext
{
    internal CustomComparer Sut { get; private set; } = null!;
    public void Setup()
    {
        Sut = CustomComparer.Default;
    }
}