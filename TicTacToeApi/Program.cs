using TicTacToeApi.Hubs;
using TicTacToeApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR with proper configuration
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

// Add MongoDB Service
builder.Services.AddSingleton<IMongoDBService, MongoDBService>();

// Add CORS with proper SignalR support
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5216")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Check MongoDB connection
var mongoService = app.Services.GetRequiredService<IMongoDBService>();
if (!await mongoService.IsConnectedAsync())
{
    app.Logger.LogCritical("Failed to connect to MongoDB. Please ensure MongoDB is running.");
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Use CORS before routing
app.UseCors("SignalRPolicy");

app.UseRouting();
app.UseAuthorization();

// Map endpoints after UseRouting
app.MapControllers();
app.MapHub<GameHub>("/gamehub");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
