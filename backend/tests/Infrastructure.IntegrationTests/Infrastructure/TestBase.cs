namespace SchoolClubs.Infrastructure.IntegrationTests.Infrastructure;

public abstract class TestBase
{
    protected HttpClient Client { get; private set; } = null!;

    [SetUp]
    public async Task SetUp()
    {
        if (FunctionalTestSetup.DbResetter is not null) await FunctionalTestSetup.DbResetter.ResetAsync();
        Client = FunctionalTestSetup.Factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        Client.Dispose();
    }
}
