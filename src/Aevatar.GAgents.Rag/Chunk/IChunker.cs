using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aevatar.GAgents.Chunk;

public interface IChunker {
    public Task<List<string>> Chunk(string text, int maxChunkSize);
}