using MediatR;
using VideoSaaS.Application.Videos.Contracts;

namespace VideoSaaS.Application.Videos.GenerateVideo;

public sealed record GenerateVideoCommand(VideoGenerationRequest Request) : IRequest<VideoJobDto>;
