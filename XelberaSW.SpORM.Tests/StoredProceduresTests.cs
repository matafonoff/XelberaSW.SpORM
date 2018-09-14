using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XelberaSW.SpORM.Tests.TestInternals;
using XelberaSW.SpORM.Tests.TestInternals.DbContext;

namespace XelberaSW.SpORM.Tests
{
    [TestClass]
    public class StoredProceduresTests : DbRelatedTestBase
    {
        [TestMethod]
        public async Task AirTerminalTest()
        {
            using (var ctx = GetRequiredService<MyDbContext>())
            {
                var result = await ctx.AirTerminal(6304532);

                Assert.IsNotNull(result);
                Assert.IsTrue(!string.IsNullOrEmpty(result.NameFull));
                Assert.IsTrue(result.Distance > 0);
                Assert.IsTrue(result.LocationId != 0);
            }
        }

        [TestMethod]
        public async Task AirTerminalForNonExistingCityTest()
        {
            using (var ctx = GetRequiredService<MyDbContext>())
            {
                var result = await ctx.AirTerminal(0);

                Assert.IsNull(result);
            }
        }


        [TestMethod]
        public async Task LocationRouteTest()
        {
            using (var ctx = GetRequiredService<MyDbContext>())
            {
                var result = await ctx.LocationRouteList(6418769, 6309333);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Locations);
                Assert.IsInstanceOfType(result.Locations, typeof(List<LocationInfo>));
                Assert.AreEqual(result.Locations.Count, 4);
            }
        }

        [TestMethod]
        public async Task LocationRouteTestForNonExistingRoute()
        {
            using (var ctx = GetRequiredService<MyDbContext>())
            {
                var result = await ctx.LocationRouteList(0, 0);

                Assert.IsNotNull(result);
                Assert.IsNull(result.Locations);
                Assert.IsNull(result.Summary);
            }
        }


        [TestMethod]
        public async Task GetTariffTest()
        {
            using (var ctx = GetRequiredService<MyDbContext>())
            {
                var result = await ctx.GetTariff(6304532, 6309333);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Summary);
                Assert.IsNotNull(result.MovementRateEntities);
                Assert.IsNotNull(result.RateEntries);

                Assert.IsTrue(result.RateEntries.Count > 0);
                Assert.IsTrue(result.MovementRateEntities.Count > 0);
            }
        }

        [TestMethod]
        public async Task GetTariffTestForNonExistingRoute()
        {
            using (var ctx = GetRequiredService<MyDbContext>())
            {
                var result = await ctx.GetTariff(0, 0);

                Assert.IsNotNull(result);
            }
        }

        [TestMethod]
        public async Task GetStreetListAsyncTest()
        {
            using (var ctx = GetRequiredService<MyDbContext>())
            {
                var result = await ctx.GetStreetListAsync(6304532);

                Assert.IsNotNull(result);
            }
        }

        [TestMethod]
        public async Task ChangeSessionStateTest()
        {
            using (var ctx = GetRequiredService<MyDbContext>())
            {
                var newStatus = UserStatus.Lunch;

                var result = await ctx.ChangeSessionStateAsync(newStatus);

                Assert.IsNotNull(result);
                Assert.AreEqual(result.NewUserStatus, newStatus);
            }
        }


        [TestMethod]
        public async Task CustomParamConverterTest()
        {
            using (var ctx = GetRequiredService<MyDbContext>())
            {
                var result = await ctx.CustomConverterTest("6304532");

                Assert.IsNotNull(result);
            }
        }

        private string RequestString => "<Request><Page>1</Page><PageSize>15</PageSize><Sorts><Member>Date</Member><SortDirection>Descending</SortDirection></Sorts><Filters><ConvertedValue>Мск1801105312</ConvertedValue><Member>Number</Member><MemberType /><Operator>StartsWith</Operator><Value>Мск1801105312</Value></Filters></Request>";

        [TestMethod]
        public async Task ExecRequestWithMetadataTest()
        {
            using (var ctx = GetRequiredService<MyDbContext>())
            {
                string requestParams = RequestString;
                string contractorTemplate = "";
                string phoneTemplate = "";
                var result = await ctx.GetDocumentsByAsync(requestParams, contractorTemplate, phoneTemplate);

                Assert.IsNotNull(result);
                Assert.IsInstanceOfType(result, typeof(DataSourceResult<DocumentDetailedInfo>));
                Assert.IsNotNull(result.Data);
                Assert.IsTrue(result.Data.Count > 0);
                Assert.IsTrue(result.Total > 0);
            }
        }


        [TestMethod]
        public async Task GetTripDetailedInfoAsync_1()
        {
            using (var ctx = GetRequiredService<MyDbContext>())
            {
                var result = await ctx.GetTripDetailedInfoAsync(69907018);

                Assert.IsNotNull(result);
            }
        }
        [TestMethod]
        public async Task GetTripDetailedInfoAsync_2()
        {
            using (var ctx = GetRequiredService<MyDbContext>())
            {
                var result = await ctx.GetTripDetailedInfoAsync(69911693);

                Assert.IsNotNull(result);
            }
        }

        [TestMethod]
        public async Task CreateDeliveryTest()
        {
            DeliveryInfo deliveryInfo = new DeliveryInfo
            {
                CargoInDocumentId = null,
                CostDelivery = 0,
                CostLoaderDelivery = 0,
                Date = new System.DateTimeOffset(),
                DeliveryAddress = "",
                DeliveryAddressId = null,
                DeliveryCargoDimentions = "1.2 x 0.8 x 1.2",
                DeliveryCargoUnloadTypeId = null,
                DeliveryCargoUnloadWorkman = 0,
                DeliveryContactPerson = "",
                DeliveryDateExecute = null,
                DeliveryTerminalId = 120426,
                DeliveryTimeLunch = "",
                DeliveryTimeRunning = "",
                DeliveryUserId = null,
                DocumentId = 69996636,
                LocationToId = 6309451,
                Note = "12345",
                PaymentMethod = 0,
                StateId = 6,
            };

            using (var ctx = GetRequiredService<MyDbContext>())
            {
                var result = await ctx.CreateDeliveryAsync(deliveryInfo);

                Assert.IsNotNull(result);
                Assert.IsTrue(result.DocumentId > 0);
            }
        }

        [TestMethod]
        public async Task GetEmailMessageBodyAsyncTest()
        {
            using (var ctx = GetRequiredService<MyDbContext>())
            {
                var result = await ctx.GetEmailMessageBodyAsync();

                Assert.IsNotNull(result);
            }
        }
        [TestMethod]
        public async Task GetUserListAsyncTest()
        {
            using (var ctx = GetRequiredService<MyDbContext>())
            {
                var result = await ctx.GetUserListAsync(new UserListQueryParameters
                {
                    IsOperator = true
                });

                Assert.IsNotNull(result);
            }
        }
        [TestMethod]
        public async Task CreateCommentTest()
        {
            var commentInfo = new CommentInfo
            {
                Note = "Happy New Year",
                DocumentId = 70003763,
                CargoStateId = 4
            };

            using (var ctx = GetRequiredService<MyDbContext>())
            {
                var result = await ctx.CreateCommentAsync(commentInfo);

                Assert.IsNotNull(result);
            }
        }

        [TestMethod]
        public async Task ContractorPaymentListTest()
        {
            var request = new InvoiceSearchRequest
            {
                ContractorId = 396176,
                PageSize = 20,
                PageNumber = 1,
            };

            using (var ctx = GetRequiredService<MyDbContext>())
            {
                var result = await ctx.ContractorPaymentListAsync(request);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.Invoices);
                Assert.IsTrue(result.Invoices.Count > 0);
            }
        }

        [TestMethod]
        public async Task SendEmailMessage()
        {
            using (var ctx = GetRequiredService<MyDbContext>())
            {
                var emailMessageRequest = new EmailMessageRequest
                {
                    DocumentId = 70003763,
                    Email = "rybalko.a@ratek.su",
                    Body = "Happy New Year"
                };

                try
                {
                    await ctx.SendEmailMessageAsync(emailMessageRequest);
                }
                catch (SpExecutionException ex)
                {
                    // TODO fix in database
                    if (ex.Message != "Документ поставлен в очередь на отправку.")
                    {
                        throw;
                    }
                }
            }
        }
    }
}
