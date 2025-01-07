using System;
using System.Collections.Generic;
using System.Text;

namespace AevatarGAgents.NamingContest.Common;

public static class AssembleMessageUtil
{
    public static string AssembleNamingContent(string agentName, string naming)
    {
        return $"{agentName} named the above information is: \"{naming}\".";
    }

    public static string AssembleDebateContent(string agentName, string debateContent)
    {
        return $"{agentName} debate is: \"{debateContent}\".";
    }

    public static string AssembleSummaryBeforeStep(List<Tuple<string, string>> creativeNamingList, string summary,
        string describe)
    {
        var content = new StringBuilder();
        foreach (var creativeNaming in creativeNamingList)
        {
            content.AppendLine($"{creativeNaming.Item1} naming is:{creativeNaming.Item2}");
        }

        return
            $"""
             The naming theme is: {describe}

             Based on the summarized experience from the last round, here are the insights:
             {summary}
              
             The following are your teammates and the names they have provided for the naming theme:
             {content.ToString()}
             """;
    }

    public static string AssembleDiscussionContent(string agentName, string debateContent)
    {
        return $"{agentName} say: \"{debateContent}\".";
    }

    public static string AssembleDiscussionSummary(string naming, string reason)
    {
        return $"""
                After the above discussion, we decided to use: {naming} as our chosen name. The reasons are as follows:
                {reason}
                """;
    }

    public static string AssembleJudgeAsking(string judgeName, string question)
    {
        return $"Judge {judgeName}'s question is: {question}";
    }

    public static string AssembleCreativeAnswer(string agentName, string replay)
    {
        return $"Team member {agentName}'s answer to the above question is:{replay}";
    }
}