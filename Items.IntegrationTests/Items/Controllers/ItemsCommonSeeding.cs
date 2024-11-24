using Items.Domain;
using System.Collections.Generic;


namespace Items.IntegrationTests.AdminPortal.Controllers
{
    internal class ItemsCommonSeeding
    {
        internal static void SeedItemsList(ItemsContext context, out List<Domain.DomainEntities.Item> items)
        {
            items = new List<Domain.DomainEntities.Item>();

            Domain.DomainEntities.Item item = null;
            for (int i = 0; i < 2; i++)
            {
                item = new Domain.DomainEntities.Item
                {
                    Code = i,
                    Value = "string" + (i + 1)
                };
                context.Items.Add(item);
                items.Add(item);
            }

            context.SaveChanges();
        }
    }
}