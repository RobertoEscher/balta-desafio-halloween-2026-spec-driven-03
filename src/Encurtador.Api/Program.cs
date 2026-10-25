using Encurtador.Application.Links;
using Encurtador.Api.Endpoints;
using Encurtador.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var publicBaseUrl = builder.Configuration["PublicBaseUrl"];
if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var configuredBaseUrl)
    || (configuredBaseUrl.Scheme != Uri.UriSchemeHttp && configuredBaseUrl.Scheme != Uri.UriSchemeHttps))
{
    throw new InvalidOperationException("PublicBaseUrl must be configured as an absolute HTTP or HTTPS URL.");
}

builder.Services.AddProblemDetails();
builder.Services.AddScoped<LinkService>();
builder.Services.AddSingleton<ILinkCodeGenerator, LinkCodeGenerator>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseExceptionHandler();
app.MapLinksEndpoints();
app.Run();

public partial class Program { }
