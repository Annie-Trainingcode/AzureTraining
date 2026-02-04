using EventHubConsumer;
using EventHubConsumer.Services;

var builder = Host.CreateApplicationBuilder(args);

// Register services
builder.Services.AddSingleton<IMp3FileReassemblyService, Mp3FileReassemblyService>();
builder.Services.AddHostedService<EventHubConsumerService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
