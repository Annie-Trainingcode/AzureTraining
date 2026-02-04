using AuctionService.Data;
using AuctionService.Dto;
using AuctionService.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Azure;
using BpMessageBus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuctionController : ControllerBase
    {
        private readonly AuctionContext _context;
        private readonly IMapper _mapper;
        private readonly ResponseDto _response;
        private readonly IMessageBus _messageBus;
        private readonly IConfiguration _config;
        public AuctionController(AuctionContext context, IMapper mapper, IMessageBus messageBus,
            IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _response = new ResponseDto();
            _messageBus = messageBus;
            _config = configuration;
        }

        [HttpGet]
        //  [Authorize]
        public async Task<ActionResult<ResponseDto>> GetAuctions()
        {
            try
            {
                var auctions = await _context.Auctions.Include(x => x.Item).OrderBy(x => x.Item.Make).ToListAsync();
                var resContent = _mapper.Map<List<AuctionDto>>(auctions);
                _response.Result = resContent;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
            }
            return _response;
        }
        //changes for consumer microservice search
        //[HttpGet]
        //public async Task<ActionResult<List<AuctionDto>>> GetAuctions(string date)
        //{
        //    //var auctions = await _context.Auctions.Include(x => x.Item).OrderBy(x => x.Item.Make).ToListAsync();
        //    var query = _context.Auctions.OrderBy(x => x.Item.Make).AsQueryable();
        //    if(!string.IsNullOrEmpty(date))
        //    {
        //        query=query.Where(x=>x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
        //    }
        //    //return _mapper.Map<List<AuctionDto>>(auctions);
        //    return await query.ProjectTo<AuctionDto>(_mapper.ConfigurationProvider).ToListAsync();
        //}
        [HttpGet("{id}")]
        public async Task<ActionResult<AuctionDto>> GetAuction(Guid id)
        {
            var auction = await _context.Auctions.Include(x => x.Item).FirstOrDefaultAsync(x => x.Id == id);
            return _mapper.Map<AuctionDto>(auction);
        }
        [HttpPost]
        //public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
        //{
        //    var auction=_mapper.Map<Auction>(auctionDto);
        //    auction.Seller = "Seller1";
        //    _context.Auctions.Add(auction);
        //    var res=await _context.SaveChangesAsync()>0;
        //    if(!res)return BadRequest("Data not saved");
        //    return CreatedAtAction(nameof(GetAuction),new { auction.Id },_mapper.Map<AuctionDto>(auction));
        //}
      //  [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ResponseDto>> CreateAuction(CreateAuctionDto auctionDto)

        {
            try
            {
                var auction = _mapper.Map<Auction>(auctionDto);
                auction.Seller = "Test";
                _context.Auctions.Add(auction);
                var res = await _context.SaveChangesAsync() > 0;
                if (!res) { _response.IsSuccess = false; return BadRequest(_response); }
                var resContent = _mapper.Map<AuctionDto>(auction);
                _response.IsSuccess = true;
                _response.Result = resContent;
                return _response;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;

            }
            return _response;

            // return CreatedAtAction(nameof(GetAuction), new {auction.Id}, _mapper.Map<AuctionDto>(auction));
        }
        [Route("EmailAuctionRequest")]
        [HttpPost]
        public async Task<object> EmailAuction([FromBody] AuctionDto auctionDto)

        {
            await _messageBus.PublishMessage(auctionDto, _config.GetValue<string>("TopicAndQueueName:EmailAuctionMessage"));

            try
            {
                _response.Result = true;
            }
            catch (Exception ex)
            {
                _response.Message = ex.Message;
                _response.IsSuccess = false;
            }
            return _response;
        }


    }
}
