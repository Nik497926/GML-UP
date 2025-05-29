using Gml.Web.Api.Core.Extensions;
using Gml.Web.Api.Core.Services;

var builder = WebApplication.CreateBuilder(args);
builder.RegisterServices();
builder.Services.AddScoped<FileSystemService>();

var app = builder.Build();
app.RegisterServices();

app.Run();
