using MediatR;
using VideoSaaS.Application.Videos.Contracts;

namespace VideoSaaS.Application.Videos.GetVideo;

public sealed record GetVideoQuery(Guid Id) : IRequest<VideoJobDto?>;
