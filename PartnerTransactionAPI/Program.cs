using log4net;
using log4net.Config;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// --- Setup log4net ---
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
var configFile = new FileInfo("log4net.config");

if (!configFile.Exists)
{
    Console.WriteLine("⚠️ log4net.config file not found at: " + configFile.FullName);
}
else
{
    XmlConfigurator.Configure(logRepository, configFile);
    Console.WriteLine("✅ log4net.config loaded successfully from: " + configFile.FullName);
}

// --- Test Log ---
var log = LogManager.GetLogger(typeof(Program));
log.Info("===== Test Log4Net: System started successfully =====");

// --- Build API ---
builder.Services.AddControllers();
var app = builder.Build();

// Swagger service
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddControllers();

app.UseAuthorization();
app.MapControllers();
app.Run();


app.UseAuthorization();
app.MapControllers();
app.Run();
log.Info("===== Log after app.Run() triggered =====");
