using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class PlacesCatalogService : IPlacesCatalogService
{
    public BaseDictionary<string, HotScenesController.PlaceInfo> PlacesCatalog { get; }

    private readonly IPlacesAPIController placesAPIController;

    public PlacesCatalogService(IPlacesAPIController placesAPIController)
    {
        this.placesAPIController = placesAPIController;
    }

    public async UniTask<IReadOnlyList<HotScenesController.PlaceInfo>> RequestPlacesAsync(int pageNumber, int pageSize, bool cleanCachedPages, CancellationToken ct)
    {
        List<HotScenesController.PlaceInfo> placesFromPlacesAPI = await placesAPIController.GetPlacesFromPlacesAPI();
        return null;
    }

    public void AddPlacesToCatalog(BaseDictionary<string, HotScenesController.PlaceInfo> placeItems)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }
}
