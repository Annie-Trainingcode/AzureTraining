using EmailBpService.Dto;

namespace EmailBpService.Service
{
    public interface IEmailService
    {
        Task EmailAuctionLog(AuctionDto auctionDto);
    }
}
