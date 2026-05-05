using MediatR;

namespace VideoSaaS.Application.Videos.DeleteVideo;

public sealed record DeleteVideoCommand(Guid Id) : IRequest;
