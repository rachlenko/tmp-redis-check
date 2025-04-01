using Microsoft.AspNetCore.Mvc;
using Prometheus;
using System.Diagnostics;
using MemoryTester;
using StackExchange.Redis;
using System.Net;

//var file = Path.Combine(Environment.GetEnvironmentVariable("TEST_PATH"), "test1.txt");
//File.WriteAllText(file, "test");
//Console.WriteLine("file created " + file);


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(op=>
{
    op.AddServer(new Microsoft.OpenApi.Models.OpenApiServer { Url=""});
    op.AddServer(new Microsoft.OpenApi.Models.OpenApiServer { Url = "/general/api/memorytester" });
});

builder.Services.AddSingleton<CacheService>();

var connectionMultiplexer = (IConnectionMultiplexer)ConnectionMultiplexer.Connect(builder.Configuration.GetValue<string>("redis_connection"));
builder.Services.AddSingleton(connectionMultiplexer);
builder.Services.AddSingleton<ILocker, Locker>();


//bool telemetryEnabled = builder.Configuration.GetValue<bool>("Observability:Enabled");
//if (telemetryEnabled)
//{
//    builder.Services.AddDSOpenTelServices(builder.Configuration.GetSection("Observability").Get<OpenTelSettings>());
//}


//builder.Services.AddIdentityApiEndpoints<IdentityUser>();


//builder.Services.ConfigureAll<BearerTokenOptions>(option =>
//{
//    option.BearerTokenExpiration = TimeSpan.FromMinutes(1);
//});


//builder.Services.AddAuthorization();



var app = builder.Build();



app.UseSwagger();
app.UseSwaggerUI();




app.MapControllers();
app.UseRouting();
//app.MapIdentityApi<IdentityUser>();

//app.UseAuthentication();
//app.UseAuthorization();


app.MapPost("/lock/{key}", async (ILocker locker,string key, CancellationToken ct) =>
{
    try
    {
        locker.TrySet(key);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.Problem(new ProblemDetails { Status = (int)HttpStatusCode.InternalServerError,Detail = ex.Message });
    }
})
.WithName("lock")
.WithOpenApi();




app.MapGet("/mymetric", async (CancellationToken ct) =>
{
    var process = Process.GetCurrentProcess();
    var gcInfo = GC.GetGCMemoryInfo();
    

    return Results.Ok(new
    {
        AllocatedMemoryHeap = GC.GetTotalMemory(false) / 1024 / 1024,
        WorkingSet64 = process.WorkingSet64 / 1024 / 1024,
        gcInfo.TotalAvailableMemoryBytes,
        gcInfo.HighMemoryLoadThresholdBytes,
        gcInfo.TotalCommittedBytes
    });


})
.WithName("mymetric")
.WithOpenApi();

app.MapPost("/gc", async (CancellationToken ct) =>
{

    GC.Collect();



    return Results.Ok();


})
.WithName("gc")
.WithOpenApi();


app.MapPost("/allocate", async ([FromBody] int mb, CacheService cacheService, CancellationToken ct) =>
{

    cacheService.addMb(mb);

    return Results.Ok();


})
.WithName("allocate")
.WithOpenApi();

app.MapPost("/clear", async (CacheService cacheService, CancellationToken ct) =>
{

    cacheService.Clear();

    return Results.Ok();


})
.WithName("clear")
.WithOpenApi();

app.UseMetricServer();



await app.RunAsync();




class CacheService
{
    private List<byte[]> Data = new List<byte[]>();


    public void addMb(int mb)
    {
        var array = new byte[1024 * 1024 * mb];

        Data.Add(array);
    }


    public void Clear()
    {
        Data.Clear();   
    }

}

//// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
//var maxMbToAdd = int.Parse(Environment.GetEnvironmentVariable("MAX_MB_TO_ADD"));

//var delay = TimeSpan.Parse(Environment.GetEnvironmentVariable("DELAY_ADD"));

//Console.WriteLine($"Allocated memory: {GC.GetTotalMemory(false)}");
//var data = new List<byte[]>();

//for (int i = 0; i < maxMbToAdd; i++)
//{
//    try
//    {
//        var array = new byte[1024 * 1024];
//        data.Add(array);

//        Console.WriteLine($"Adding: {i + 1} Mb");
//        Console.WriteLine($"Allocated memory: {GC.GetTotalMemory(false)}");
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"unable to add more memory");
//        Console.WriteLine(ex.Message);
//    }


//    Thread.Sleep(delay);
//}

//Console.WriteLine($"Allocated memory: {GC.GetTotalMemory(false)}");











//Console.ReadLine();


