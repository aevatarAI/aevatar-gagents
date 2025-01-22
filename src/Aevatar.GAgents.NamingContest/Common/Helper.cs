using System;
using System.Security.Cryptography;
using System.Text;

namespace Aevatar.GAgents.NamingContest.Common;

public static class Helper
{
    public static Guid GetVoteCharmingGrainId(int round,int step)
    {
        return ConvertStringToGuid($"AI-Most-Charming-Naming-Contest-{round}-{step}");
    }
    
    public static Guid GetHostGroupGrainId()
    {
        return ConvertStringToGuid(string.Concat("AI-Most-Charming-Naming-Contest-Host-Group"));
    }
    
    private static Guid ConvertStringToGuid(string input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(hash);
        }
    }
}