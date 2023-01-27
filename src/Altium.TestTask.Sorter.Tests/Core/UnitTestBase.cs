namespace Altium.TestTask.Sorter.Tests.Core;

public abstract class UnitTestBase<T> where T : class, IUnitTestContext, new()
{
    protected T Context { get; private set; } = null!;

    [SetUp]
    public void Setup()
    {
        this.Context = new T();
        this.Context.Setup();
    }

    [TearDown]
    public void TearDown()
    {
        this.Context?.TearDown();
    }
}