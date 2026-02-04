
using AuctionService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Data
{
    public class DbInitializer
    {
        public static void InitDb(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            SeedData(scope.ServiceProvider.GetService<AuctionContext>());
        }

        private static void SeedData(AuctionContext? context)
        {
            context.Database.Migrate();
            if (context.Auctions.Any())
            {
                Console.WriteLine("already data");
                return;
            }
            var auctions = new List<Auction>()
            {
                 new Auction()
            {
                Id=Guid.NewGuid(),
                Status=Status.Live,
                ReservePrice=1000,
                Winner="a",
                Seller="Wipro",
                AuctionEnd=DateTime.Now.AddDays(7),
                Item=new Item() {
                Make="HP",
                Model="Intel",
                Mileage=234,
                Color="grey",
                Year=2009,
                ImageUrl="https://cdn.pixabay.com/photo/2024/02/21/15/28/dahlia-8587940_1280.jpg"
                }
                },

            new Auction()
            {
                Id=Guid.NewGuid(),
                Status=Status.Live,
                ReservePrice=2000,
                Seller="ABC",
                AuctionEnd=DateTime.Now.AddDays(7),
                Item=new Item() {
                Make="Dell",
                Model="I7",
                Mileage=200,
                Color="black",
                Year=2019,
                ImageUrl="https://cdn.pixabay.com/photo/2015/04/19/08/32/marguerite-729510_1280.jpg"

                }
            }
            };
            context.AddRange(auctions);
            context.SaveChanges();
        }
    }
}
