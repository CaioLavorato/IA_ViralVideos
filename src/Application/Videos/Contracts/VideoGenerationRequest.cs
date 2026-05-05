namespace VideoSaaS.Application.Videos.Contracts;

public sealed record VideoGenerationRequest(
    string Theme,
    string Style,
    string Duration,
    string Tone,
    string Voice,
    int SceneCount,
    string ImageType,
    string Format);
