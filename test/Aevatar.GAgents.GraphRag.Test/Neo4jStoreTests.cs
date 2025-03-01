using Aevatar.GAgents.GraphRag.Abstractions;
using Aevatar.GAgents.GraphRag.Abstractions.Models;
using Aevatar.GAgents.Neo4jStore.GraphRagStore;
using Microsoft.Extensions.Logging;
using Moq;
using Neo4j.Driver;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.GraphRag.Test;


public class Neo4JStoreTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IDriver> _driverMock;
    private readonly Mock<IAsyncSession> _sessionMock;
    private readonly Neo4JStore _store;
    private readonly Mock<ILogger<Neo4JStore>> _mockLogger;
    
    public Neo4JStoreTests(ITestOutputHelper output)
    {
        _output = output;
        
        _driverMock = new Mock<IDriver>();
        _sessionMock = new Mock<IAsyncSession>();
        _mockLogger = new Mock<ILogger<Neo4JStore>>();
        
        _driverMock.Setup(d => d.AsyncSession())
            .Returns(_sessionMock.Object);
        
        _store = new Neo4JStore(_driverMock.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task StoreAsync_ShouldExecuteCorrectCypher()
    {
        // Arrange
        var node = new Node { Id = "test-node" };
        var relationship = new Relationship { Type = "TEST_REL" };
        var parametersCaptured = new List<object>();
        
        _sessionMock.Setup(s => s.ExecuteWriteAsync(
                It.IsAny<Func<IAsyncQueryRunner, Task>>(),
                It.IsAny<Action<TransactionConfigBuilder>>()))
            .Callback<Func<IAsyncTransaction, Task>, Action<TransactionConfigBuilder>>(
                (func, config) => parametersCaptured.Add(func.Target))
            .Returns(Task.CompletedTask);

        // Act
        await _store.StoreAsync(node, relationship);

        // Assert
        _sessionMock.Verify(s => s.ExecuteWriteAsync(
            It.IsAny<Func<IAsyncQueryRunner, Task>>(),
            It.IsAny<Action<TransactionConfigBuilder>>()), Times.Once);
    }

    [Fact]
    public async Task RetrieveAsync_ShouldReturnNode()
    {
        // Arrange
        const string nodeId = "test-node";
        var expectedNode = new Node { Id = nodeId };
        var recordMock = new Mock<IRecord>();
        var nodeMock = new Mock<INode>();

        nodeMock.SetupGet(n => n.Properties)
            .Returns(new Dictionary<string, object> { ["id"] = nodeId });
        nodeMock.SetupGet(n => n.Labels)
            .Returns(new List<string> { "Test" });
        
        recordMock.Setup(r => r["n"])
            .Returns(nodeMock.Object);
        
        var resultCursorMock = new Mock<IResultCursor>();
        resultCursorMock.SetupSequence(r => r.FetchAsync())
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        resultCursorMock.Setup(r => r.Current)
            .Returns(recordMock.Object);

        _sessionMock.Setup(s => s.ExecuteReadAsync(
                It.IsAny<Func<IAsyncQueryRunner, Task<Node>>>(),
                It.IsAny<Action<TransactionConfigBuilder>>()))
            .ReturnsAsync(expectedNode);

        // Act
        var result = await _store.RetrieveAsync(nodeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nodeId, result.Id);
        _sessionMock.Verify(s => s.ExecuteReadAsync(
            It.IsAny<Func<IAsyncQueryRunner, Task<Node>>>(),
            It.IsAny<Action<TransactionConfigBuilder>>()), Times.Once);
    }

    [Fact]
    public async Task QueryAsync_ShouldReturnResults()
    {
        // Arrange
        const string cypher = "MATCH (n) RETURN n";
        var expectedResults = new List<QueryResult>
        {
            new QueryResult { Data = new Dictionary<string, object> { ["n"] = "value1" } },
            new QueryResult { Data = new Dictionary<string, object> { ["n"] = "value2" } }
        };

        var resultCursorMock = new Mock<IResultCursor>();
        resultCursorMock.SetupSequence(r => r.FetchAsync())
            .ReturnsAsync(true)
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        
        var records = new List<IRecord>
        {
            new MockRecord(new Dictionary<string, object> { ["n"] = "value1" }),
            new MockRecord(new Dictionary<string, object> { ["n"] = "value2" })
        };
        
        var index = 0;
        resultCursorMock.Setup(r => r.Current)
            .Returns(() => records[index++]);

        _sessionMock.Setup(s => s.ExecuteReadAsync(
                It.IsAny<Func<IAsyncQueryRunner, Task<List<QueryResult>>>>(),
                It.IsAny<Action<TransactionConfigBuilder>>()))
            .ReturnsAsync(expectedResults);

        // Act
        var results = await _store.QueryAsync(cypher);

        // Assert
        Assert.Equal(2, results.Count());
        _sessionMock.Verify(s => s.ExecuteReadAsync(
            It.IsAny<Func<IAsyncQueryRunner, Task<List<QueryResult>>>>(),
            It.IsAny<Action<TransactionConfigBuilder>>()), Times.Once);
    }
    

    [Fact]
    public async Task DeleteAsync_ShouldExecuteDeleteCypher()
    {
        // Arrange
        const string nodeId = "delete-node";
        var parametersCaptured = new List<object>();

        _sessionMock.Setup(s => s.ExecuteWriteAsync(
                It.IsAny<Func<IAsyncQueryRunner, Task>>(),
                It.IsAny<Action<TransactionConfigBuilder>>()))
            .Callback<Func<IAsyncTransaction, Task>, Action<TransactionConfigBuilder>>(
                (func, config) => parametersCaptured.Add(func.Target))
            .Returns(Task.CompletedTask);

        // Act
        await _store.DeleteAsync(nodeId);

        // Assert
        _sessionMock.Verify(s => s.ExecuteWriteAsync(
            It.IsAny<Func<IAsyncQueryRunner, Task>>(),
            It.IsAny<Action<TransactionConfigBuilder>>()), Times.Once);
    }
    
    private class MockRecord : IRecord
    {
        private readonly Dictionary<string, object> _values;

        public MockRecord(Dictionary<string, object> values)
        {
            _values = values;
        }

        public object this[int index] => throw new NotImplementedException();
        public object this[string key] => _values[key];
        public IReadOnlyDictionary<string, object> Values => _values;
        public IReadOnlyList<string> Keys => _values.Keys.ToList();
    }
}