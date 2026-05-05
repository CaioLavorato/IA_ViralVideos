using VideoSaaS.Domain.ValueObjects;

namespace VideoSaaS.Application.Videos.Contracts;

public sealed record GeneratedScriptDto(List<SceneSpec> Scenes, string RawJson);
