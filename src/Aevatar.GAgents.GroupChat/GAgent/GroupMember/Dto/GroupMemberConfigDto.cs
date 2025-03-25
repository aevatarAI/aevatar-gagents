using Aevatar.Core.Abstractions;

namespace GroupChat.GAgent.Dto;

[GenerateSerializer]
public class GroupMemberConfigDto:ConfigurationBase
{
    [Id(0)] public string MemberName { get; set; }
}