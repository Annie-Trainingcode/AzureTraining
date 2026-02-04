
using EmployeecosmosApi.Services;
using Microsoft.Azure.Cosmos;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<CosmosClient>(serviceProvider =>
{
    var configuration = serviceProvider.GetService<IConfiguration>();
    var connectionString = configuration?.GetConnectionString("CosmosDb");
    if (string.IsNullOrEmpty(connectionString))
    {
        // Use Cosmos DB Emulator default connection string for development
       connectionString = "AccountEndpoint=AccountEndpoint=Cosmos connection string here;";
    }
    return new CosmosClient(connectionString);
});

builder.Services.AddSingleton<ICosmosDbService>(serviceProvider =>
{
    var cosmosClient = serviceProvider.GetService<CosmosClient>();
    var configuration = serviceProvider.GetService<IConfiguration>();
    var databaseName = configuration?["CosmosDbDetail:DatabaseName"] ?? "EmployeeDB";
    var containerName = configuration?["CosmosDbDetail:ContainerName"] ?? "employees";

    return new CosmosDbService(cosmosClient!, databaseName, containerName);
});
// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            );
});
builder.Services.AddSwaggerGen();
var app = builder.Build();

// Configure the HTTP request pipeline.
/* if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
} */
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();

app.Run();
