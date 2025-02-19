using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.SemanticKernel.Model;
using Microsoft.Extensions.VectorData;

namespace Aevatar.GAgents.SemanticKernel.VectorStores.Qdrant;

internal class QdrantVectorStoreCollection : IVectorStoreCollection
{
    private readonly IVectorStoreRecordCollection<Guid, TextSnippet<Guid>> _vectorStoreRecordCollection;

    public QdrantVectorStoreCollection(IVectorStoreRecordCollection<Guid, TextSnippet<Guid>> vectorStoreRecordCollection)
    {
        _vectorStoreRecordCollection = vectorStoreRecordCollection;
    }

    public Task InitializeAsync(string collectionName)
    {
        _vectorStoreRecordCollection.CreateCollectionIfNotExistsAsync();
        return Task.CompletedTask;
    }

    public Task UploadRecordAsync(List<BrainContent> files)
    {
        throw new System.NotImplementedException();
    }
}