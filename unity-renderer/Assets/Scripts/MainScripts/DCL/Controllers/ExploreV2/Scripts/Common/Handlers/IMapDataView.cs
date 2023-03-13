using UnityEngine;

public interface IMapDataView
{
    Vector2Int baseCoord { get; }
    string name { get; }
    string creator { get; }
    string description { get; }
    void SetMinimapSceneInfo(HotScenesController.PlaceInfo sceneInfo);
    bool ContainCoords(Vector2Int coords);
}
