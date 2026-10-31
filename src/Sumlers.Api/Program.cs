using Sumlers.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSumlersServices(builder.Configuration);

var app = builder.Build();
app.UseSumlersPipeline();
app.Run();

public partial class Program;
