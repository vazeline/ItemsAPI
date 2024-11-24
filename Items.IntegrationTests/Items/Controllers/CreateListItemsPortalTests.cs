using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Common.Models;
using Items.Common.Testing.Utility;
using Items.DTO.Items.Response;
using Items.IntegrationTests.TestCommon;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Items.IntegrationTests.AdminPortal.Controllers
{
    [TestClass]
    public class CreateListItemsPortalTests : TestBase
    {
        [TestMethod]
        public async Task Verify_Create_And_List_Items()
        {
            List<Domain.DomainEntities.Item> items = null;

            var application = new ItemsTestWebApplicationFactory(
            databaseSeeder: context =>
            {
                ItemsCommonSeeding.SeedItemsList(
                    context,
                    out items);
            });

            var httpClient = application.CreateClient();

            var response = await httpClient.GetAsync(requestUri: "items/list?pageSize=10");
            await AssertionUtility.AssertHttpResponseStatusCodeAsync(response);

            var result = await response.Content.ReadFromJsonAsync<OperationResult<List<ItemDTO>>>();
            AssertionUtility.AssertOperationResultIsSuccessful(result);

            var assertionHelperResult = new OperationResult();

            assertionHelperResult
                .Validate(result.Data.Count, ValidationExtensions.Equals, items.Count)
                .Validate(result.Data[0].Value, ValidationExtensions.Equals, items[0].Value)
                .AssertIsSuccessful();
        }
    }
}
