using EmailBpService.Data;
using EmailBpService.Dto;
using EmailBpService.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text;

namespace EmailBpService.Service
{
    public class EmailLogService : IEmailService
    {
        private DbContextOptions<AppDbContext> _contextOptions;

        public EmailLogService(DbContextOptions<AppDbContext> contextOptions)
        {
            _contextOptions = contextOptions;
        }

        public async Task EmailAuctionLog(AuctionDto auctionDto)
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine("<br/>AuctionInfo");
            message.AppendLine("<br/>Auction" + auctionDto.Make + " " + auctionDto.Model);
            message.AppendLine("<br/>Auction" + auctionDto.CreatedDate.ToString() + " " + auctionDto.AuctionEnd.ToString());
            message.AppendLine("<br/>");
            await LogEmailDb(message.ToString(), "admin@gmail.com");

        }

        private async Task<bool> LogEmailDb(string message, string email)
        {
            try
            {
                EmailLogger emailLogger = new()
                {
                    Email = email,
                    Message = message,
                    EmailSent = DateTime.Now,
                };
                await using var _db = new AppDbContext(_contextOptions);
                await _db.EmailLoggers.AddAsync(emailLogger);
                await _db.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
