using Ffmpeg.Command;
using FFmpeg.Core.Interfaces;
using FFmpeg.Infrastructure.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure IIS and Kestrel for larger file uploads
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 104857600; // 100 MB
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 104857600; // 100 MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // 100 MB
});

builder.Services.AddSingleton<Ffmpeg.Command.ILogger, Logger>();

// Register FFmpeg Services
builder.Services.AddScoped<IFFmpegServiceFactory>(provider =>
{
    var logger = provider.GetRequiredService<Ffmpeg.Command.ILogger>();
    return new FFmpegServiceFactory(builder.Configuration, logger);
});

// Add file service for handling temporary files
builder.Services.AddScoped<IFileService, FileService>();


var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure FFmpeg directories exist
EnsureFFmpegDirectories(app.Configuration);

app.MapGet("/", () => { return "FFmpeg API is running"; });
app.Run();

void EnsureFFmpegDirectories(IConfiguration configuration)
{
    try
    {
        var basePath = Environment.ExpandEnvironmentVariables(configuration["FFmpeg:Path"] ?? string.Empty);

        if (!string.IsNullOrEmpty(basePath))
        {
            Directory.CreateDirectory(Path.Combine(basePath, "Input"));
            Directory.CreateDirectory(Path.Combine(basePath, "Output"));
            Directory.CreateDirectory(Path.Combine(basePath, "Temp"));
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error creating FFmpeg directories");
    }
}