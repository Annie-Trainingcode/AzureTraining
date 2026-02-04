using AuctionService.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace AuctionService.Data
{
    public class AuctionContext : DbContext
    {
        public AuctionContext(DbContextOptions<AuctionContext> options)
            : base(options)
        {

        }
        public DbSet<Auction> Auctions { get; set; }
    }
}
