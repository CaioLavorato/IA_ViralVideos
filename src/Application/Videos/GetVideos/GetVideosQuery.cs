using MediatR;
using VideoSaaS.Application.Videos.Contracts;

namespace VideoSaaS.Application.Videos.GetVideos;

public sealed record GetVideosQuery : IRequest<IReadOnlyList<VideoJobDto>>;
