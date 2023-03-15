using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

public interface IPlacesCatalogService
{

    BaseDictionary<string, HotScenesController.PlaceInfo> PlacesCatalog { get; }

    UniTask<IReadOnlyList<HotScenesController.PlaceInfo>> RequestPlacesAsync(int pageNumber, int pageSize, bool cleanCachedPages, CancellationToken ct);
    void AddPlacesToCatalog(BaseDictionary<string, HotScenesController.PlaceInfo> placeItems);
    void Clear();
}
