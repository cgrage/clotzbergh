using NUnit.Framework;
using UnityEngine;

public class TerrainChunkStoreTests : IAsyncTerrainOps
{
    private bool _isRequestWorldDataCalled;
    private Vector3Int _firstReqCoords;

    [Test]
    public void WorldLoadingPrioTest()
    {
        TerrainChunkStore store = new() { AsyncTerrainOps = this };
        _isRequestWorldDataCalled = false;

        store.OnViewerMoved(new(42, 42, 42));
        store.OnUpdate();

        Assert.IsTrue(_isRequestWorldDataCalled, "IAsyncTerrainOps.RequestWorldData must be called at least once");
        Assert.AreEqual(_firstReqCoords, new Vector3Int(42, 42, 42));
    }

    void IAsyncTerrainOps.RequestMeshCalc(TerrainChunk owner, WorldChunk world, int lod, ulong worldVersion)
    {

    }

    void IAsyncTerrainOps.RequestWorldData(Vector3Int coords)
    {
        if (!_isRequestWorldDataCalled)
        {
            _isRequestWorldDataCalled = true;
            _firstReqCoords = coords;
        }

    }
}
