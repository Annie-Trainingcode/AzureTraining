
using Azure.Messaging.ServiceBus;
using EmailBpService.Dto;
using EmailBpService.Service;
using Newtonsoft.Json;
using System.Text;

namespace EmailBpService.Messaging
{
    public class AzureServiceMessageConsumer : IAzureServiceBusConsumer
    {
        private readonly string serviceBusConnectionString;
        private readonly string emailauctionqueue;
        private readonly IConfiguration _config;
        private ServiceBusProcessor _emailprocessor;
        private readonly EmailLogService _emailservice;
        public AzureServiceMessageConsumer(IConfiguration configuration, EmailLogService emailService)
        {
            _config = configuration;
            serviceBusConnectionString = _config.GetValue<string>("ServiceBusConnectionString");
            emailauctionqueue = _config.GetValue<string>("TopicAndQueueName:EmailAuctionMessage");
            var client = new ServiceBusClient(serviceBusConnectionString);
            _emailprocessor = client.CreateProcessor(emailauctionqueue);
            _emailservice = emailService;
        }

        public async Task Start()
        {
            _emailprocessor.ProcessMessageAsync += OnEmailCartRequestReceived;
            _emailprocessor.ProcessErrorAsync += OnErrorHandler;
            await _emailprocessor.StartProcessingAsync();
        }
        public async Task Stop()
        {
            await _emailprocessor.StopProcessingAsync();
            await _emailprocessor.DisposeAsync();
        }
        private async Task OnErrorHandler(ProcessErrorEventArgs args)
        {
            throw new NotImplementedException();
        }

        private async Task OnEmailCartRequestReceived(ProcessMessageEventArgs args)
        {
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);
            AuctionDto objMessage = JsonConvert.DeserializeObject<AuctionDto>(body);
            try
            {
                await _emailservice.EmailAuctionLog(objMessage);
                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }



}
