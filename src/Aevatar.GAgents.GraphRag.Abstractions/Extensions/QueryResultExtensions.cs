using System.Text;
using Aevatar.GAgents.GraphRag.Abstractions.Models;

namespace Aevatar.GAgents.GraphRag.Abstractions.Extensions;

public static class QueryResultExtensions
{
    private const int DefaultTop = 5;
    
    public static string ToNaturalLanguage(this IEnumerable<QueryResult> results, int topNumber = DefaultTop)
    {
        var sb = new StringBuilder();
        sb.AppendLine("The following is graph rag data about the user's question.");
        int index = 1;
        foreach (var result in results)
        {
            sb.AppendLine($"result {index++}:");
            foreach (var kvp in result.Data)
            {
                sb.AppendLine($"- {kvp.Key}: {FormatValue(kvp.Value)}");
            }
            sb.AppendLine();

            if (index > topNumber)
            {
                break;
            }
        }
        
        return sb.ToString();
    }

    private static string FormatValue(object value)
    {
        if (value is IList<object> list)
            return string.Join(", ", list.Select(FormatValue));
        return value.ToString();
    }
}