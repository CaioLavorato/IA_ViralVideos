using MediatR;
using Serilog;
using VideoSaaS.Application;
using VideoSaaS.Application.Tenants.GetTenants;
using VideoSaaS.Application.Videos.GenerateVideo;
using VideoSaaS.Application.Videos.GetVideo;
using VideoSaaS.Application.Videos.GetVideos;
using VideoSaaS.Application.Videos.DeleteVideo;
using VideoSaaS.Infrastructure;
using VideoSaaS.Infrastructure.Persistence;
using VideoSaaS.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddHttpContextAccessor();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<VideoGenerationWorker>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy => policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .SetIsOriginAllowed(_ => true));
});

var app = builder.Build();

await DbInitializer.InitializeAsync(app.Services);

app.UseSerilogRequestLogging();
app.UseCors("frontend");
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "video-saas-api" }));

app.MapGet("/test-tts", async (VideoSaaS.Application.Abstractions.ITtsService tts, CancellationToken ct) =>
{
    var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    var jobId = Guid.NewGuid();
    var scene = new VideoSaaS.Domain.ValueObjects.SceneSpec { Index = 1, Text = "Isto é um teste da voz nativa do piper rodando no docker.", ImagePrompt = "" };
    var result = await tts.GenerateSceneAudioAsync(tenantId, jobId, scene, "pt_BR-cadu-medium", ct);
    return Results.Ok(new { file = result });
});

app.MapPost("/videos/generate", async (GenerateVideoCommand command, ISender sender, CancellationToken ct) =>
{
    var job = await sender.Send(command, ct);
    return Results.Accepted($"/videos/{job.Id}", job);
});

app.MapGet("/videos", async (ISender sender, CancellationToken ct) =>
{
    var videos = await sender.Send(new GetVideosQuery(), ct);
    return Results.Ok(videos);
});

app.MapGet("/videos/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
{
    var video = await sender.Send(new GetVideoQuery(id), ct);
    return video is null ? Results.NotFound() : Results.Ok(video);
});

app.MapDelete("/videos/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
{
    await sender.Send(new DeleteVideoCommand(id), ct);
    return Results.NoContent();
});

app.MapGet("/tenants", async (ISender sender, CancellationToken ct) =>
{
    var tenants = await sender.Send(new GetTenantsQuery(), ct);
    return Results.Ok(tenants);
});

app.Run();

public partial class Program;
