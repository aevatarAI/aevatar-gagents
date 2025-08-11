using Aevatar.GAgents.MCP.Options;

namespace Aevatar.GAgents.MCP;

public enum DefaultMCPServer
{
    // Don't need env
    Filesystem = 0,
    Memory,
    SequentialThinking,
    Fetch,
    Context7,
    Git,
    Time,

    // Need env

    /// <summary>
    /// Env key: GITHUB_PERSONAL_ACCESS_TOKEN
    /// <br/>
    /// Ref: https://github.com/modelcontextprotocol/servers-archived/tree/main/src/github
    /// </summary>
    GitHub = 1001,

    /// <summary>
    /// Env key: GITLAB_PERSONAL_ACCESS_TOKEN, GITLAB_API_URL(optional)
    /// <br/>
    /// Ref: https://github.com/modelcontextprotocol/servers-archived/tree/main/src/gitlab
    /// </summary>
    GitLab,

    /// <summary>
    /// Env key: API_KEY, API_SECRET_KEY, ACCESS_TOKEN, ACCESS_TOKEN_SECRET
    /// <br/>
    /// Ref: https://github.com/EnesCinr/twitter-mcp
    /// </summary>
    EnesCinrTwitter,
    
    /// <summary>
    /// Env key: GOOGLE_MAPS_API_KEY
    /// <br/>
    /// Ref: https://github.com/modelcontextprotocol/servers-archived/tree/main/src/google-maps
    /// </summary>
    GoogleMaps,
}

// ReSharper disable once InconsistentNaming
/// <summary>
/// Ref: https://github.com/modelcontextprotocol/servers/tree/main
/// </summary>
public static class DefaultMCPServers
{
    public const string FilesystemMCPServerName = "filesystem";
    public const string MemoryMCPServerName = "memory";
    public const string SequentialThinkingMCPServerName = "sequential-thinking";
    public const string FetchMCPServerName = "fetch";
    public const string Context7MCPServerName = "context7";
    public const string GitMCPServerName = "git";
    public const string TimeMCPServerName = "time";
    public const string GitHubMCPServerName = "github";
    public const string GitLabMCPServerName = "gitlab";
    public const string EnesCinrTwitterMCPServerName = "enescinar-twitter";
    public const string GoogleMapsMCPServerName = "google-maps";

    private static readonly Dictionary<DefaultMCPServer, string> Names = new()
    {
        [DefaultMCPServer.Filesystem] = FilesystemMCPServerName,
        [DefaultMCPServer.Memory] = MemoryMCPServerName,
        [DefaultMCPServer.SequentialThinking] = SequentialThinkingMCPServerName,
        [DefaultMCPServer.Fetch] = FetchMCPServerName,
        [DefaultMCPServer.Context7] = Context7MCPServerName,
        [DefaultMCPServer.Git] = GitMCPServerName,
        [DefaultMCPServer.Time] = TimeMCPServerName,
        [DefaultMCPServer.GitHub] = GitHubMCPServerName,
        [DefaultMCPServer.GitLab] = GitLabMCPServerName,
        [DefaultMCPServer.EnesCinrTwitter] = EnesCinrTwitterMCPServerName,
    };

    public static Dictionary<string, MCPServerConfig> Configs = new()
    {
        [FilesystemMCPServerName] = new MCPServerConfig
        {
            ServerName = FilesystemMCPServerName,
            Command = "npx",
            Args = ["-y", "@modelcontextprotocol/server-filesystem", "/tmp", "/Users", "/System/Volumes/Data/Users"],
            Description = "Provides secure file system access capabilities " +
                          "with read, write, create, and directory management " +
                          "operations. Features path validation and permission " +
                          "controls, serving as the core tool for AI assistants " +
                          "to perform file operations, supporting text file editing, " +
                          "binary file handling, and directory structure management.",
        },
        [MemoryMCPServerName] = new MCPServerConfig
        {
            ServerName = MemoryMCPServerName,
            Command = "npx",
            Args = ["-y", "@modelcontextprotocol/server-memory"],
            Description = "Knowledge graph-based memory system that " +
                          "creates and manages structured relationships " +
                          "between entities, concepts, and information. " +
                          "Supports semantic knowledge modeling, entity " +
                          "relationship mapping, and intelligent information " +
                          "retrieval through graph traversal, enabling AI " +
                          "assistants to build and navigate complex knowledge " +
                          "networks for enhanced reasoning and context understanding.",
        },
        [SequentialThinkingMCPServerName] = new MCPServerConfig
        {
            ServerName = SequentialThinkingMCPServerName,
            Command = "npx",
            Args = ["-y", "@modelcontextprotocol/server-sequential-thinking"],
            Description = "Structured reasoning framework that provides " +
                          "step-by-step problem analysis and solution methodology. " +
                          "Supports complex problem decomposition, logical chain " +
                          "construction, and reasoning process visualization, " +
                          "helping AI assistants perform deeper thinking and " +
                          "decision-making with improved accuracy and traceability."
        },
        [Context7MCPServerName] = new MCPServerConfig
        {
            ServerName = Context7MCPServerName,
            Command = "npx",
            Args = ["-y", "@upstash/context7-mcp@latest"],
            Description = "Context-aware document and code management system providing " +
                          "intelligent information organization and retrieval capabilities. " +
                          "Supports codebase analysis, document structuring, and semantic " +
                          "search, helping AI assistants better understand and process complex " +
                          "project contexts and technical documentation."
        },
        [FetchMCPServerName] = new MCPServerConfig
        {
            ServerName = FetchMCPServerName,
            Command = "uvx",
            Args = ["mcp-server-fetch"],
            Description = "Powerful network resource acquisition tool " +
                          "supporting HTTP/HTTPS requests, API calls, " +
                          "and web content scraping. Provides request " +
                          "configuration, response handling, and error " +
                          "retry mechanisms, enabling AI assistants to " +
                          "access internet resources and retrieve real-time " +
                          "information and external data."
        },
        [GitMCPServerName] = new MCPServerConfig
        {
            ServerName = GitMCPServerName,
            Command = "uvx",
            Args = ["mcp-server-git"],
            Description = "Comprehensive Git version control operations tool " +
                          "supporting repository management, branch operations, " +
                          "commit history queries, and diff comparisons. Provides " +
                          "code version tracking and collaborative development " +
                          "support, enabling AI assistants to participate in software " +
                          "development workflows and perform code management and version control."
        },
        [TimeMCPServerName] = new MCPServerConfig
        {
            ServerName = TimeMCPServerName,
            Command = "uvx",
            Args = ["mcp-server-time"],
            Description = "Comprehensive time and date management system providing real-time " +
                          "temporal operations and calculations. Supports current time retrieval, " +
                          "timezone conversions, date formatting, duration calculations, and temporal " +
                          "comparisons. Enables AI assistants to handle time-sensitive tasks, schedule " +
                          "operations, perform date arithmetic, and work with multiple time zones " +
                          "accurately for global applications and time-aware workflows."
        },
        [GitHubMCPServerName] = new MCPServerConfig
        {
            ServerName = GitHubMCPServerName,
            Command = "npx",
            Args = ["-y", "@modelcontextprotocol/server-github"],
            Env =
            {
                ["GITHUB_PERSONAL_ACCESS_TOKEN"] = "your_github_personal_access_token_here"
            }
        },
        [GitLabMCPServerName] = new MCPServerConfig
        {
            ServerName = GitLabMCPServerName,
            Command = "npx",
            Args = ["-y", "@modelcontextprotocol/server-gitlab"],
            Env =
            {
                ["GITLAB_PERSONAL_ACCESS_TOKEN"] = "your_gitlab_personal_access_token_here",
                ["GITLAB_API_URL"] = "optional_for_self_hosted_instances"
            }
        },
        [EnesCinrTwitterMCPServerName] = new MCPServerConfig
        {
            ServerName = EnesCinrTwitterMCPServerName,
            Command = "npx",
            Args = ["-y", "@enescinar/twitter-mcp"],
            Env =
            {
                ["API_KEY"] = "your_api_key_here",
                ["API_SECRET_KEY"] = "your_api_secret_key_here",
                ["ACCESS_TOKEN"] = "your_access_token_here",
                ["ACCESS_TOKEN_SECRET"] = "your_access_token_secret_here"
            }
        },
        [GoogleMapsMCPServerName] = new MCPServerConfig
        {
            ServerName = GoogleMapsMCPServerName,
            Command = "npx",
            Args = ["-y", "@modelcontextprotocol/server-google-maps"],
            Env =
            {
                ["GOOGLE_MAPS_API_KEY"] = "your_api_key_here",
            }
        },
    };

    public static string ToMCPServerName(this DefaultMCPServer mcpServer)
    {
        return Names.TryGetValue(mcpServer, out var name) ? name : mcpServer.ToString();
    }
}