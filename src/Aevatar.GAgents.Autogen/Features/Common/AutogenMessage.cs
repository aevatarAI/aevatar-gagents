using Orleans;

namespace Aevatar.GAgents.Autogen.Common;

[GenerateSerializer]
public class AutogenMessage
{
    public AutogenMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }

    [Id(0)]public string Role { get; set; }
    [Id(1)] public string Content { get; set; }
    
    
}