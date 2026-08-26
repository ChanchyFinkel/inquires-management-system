using Microsoft.EntityFrameworkCore;
using Inquiries.Api.Middleware;
using Inquiries.Data;
using Inquiries.Services;

const string AngularDevCorsPolicy = "AngularDev";

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<InquiriesDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IInquiryRepository, InquiryRepository>();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddScoped<IInquiryService, InquiryService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:4200", "https://inquiries-client-2994.azurewebsites.net")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "Inquiries API v1"));
}

app.UseHttpsRedirection();

app.UseCors(AngularDevCorsPolicy);

app.UseAuthorization();

app.MapGet("/health", async (InquiriesDbContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Database.CanConnectAsync(cancellationToken) ? Results.Ok("healthy") : Results.StatusCode(503));

app.MapControllers();

app.Run();
