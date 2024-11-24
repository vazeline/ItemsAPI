using Microsoft.VisualStudio.TestTools.UnitTesting;
using Items.Data.EFCore.Entities;

namespace Items.IntegrationTests
{
    [TestClass]
    public class TestBase
    {
        [TestInitialize]
        public void Initialize()
        {
            // We have a mixture of tests, some integration using web application factory, some unit tests using a test host builder.
            // There are two mechanisms to allow domain entities to access scoped services, one is supposed to be used for web apps, the other for console apps/unit tests.
            // Normally, when running a web app or console app directly, only one method would be in use.
            // However, when running a bunch of tests at the same time, both can end up being used within the same assembly lifetime,
            // and because the property is static, it isn't cleared between tests.
            // Therefore we need to clear them down before each test to ensure we don't get exceptions from the wrong one being accessed.
            DomainEntityBase.SetCurrentServiceProviderFunc(null);
        }
    }
}
