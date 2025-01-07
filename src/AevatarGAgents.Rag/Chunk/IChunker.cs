using System.Collections.Generic;
using System.Threading.Tasks;

namespace AevatarGAgents.Chunk;

public interface IChunker {
    public Task<List<string>> Chunk(string text, int maxChunkSize);
}