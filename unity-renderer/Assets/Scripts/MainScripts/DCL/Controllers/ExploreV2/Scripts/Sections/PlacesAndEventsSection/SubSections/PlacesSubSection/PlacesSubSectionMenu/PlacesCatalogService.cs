using Cysharp.Threading.Tasks;
using DCL;
using DCLServices.Lambdas;
using DCLServices.Lambdas.PlaceService;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class PlacesCatalogService : IPlacesCatalogService, ILambdaServiceConsumer<PlacesResponse>
{
    public BaseDictionary<string, HotScenesController.PlaceInfo> PlacesCatalog { get; }

    private const string PAGINATED_PLACES_END_POINT = "https://places.decentraland.org/";
    internal const string END_POINT = "api/places?order_by=most_active&order=desc&with_realms_detail=true";
    internal const int TIMEOUT = ILambdasService.DEFAULT_TIMEOUT;
    internal const int ATTEMPTS_NUMBER = ILambdasService.DEFAULT_ATTEMPTS_NUMBER;
    private LambdaResponsePagePointer<PlacesResponse> placesPagePointer;

    private Service<ILambdasService> lambdasService;

    public async UniTask<List<PlacesResponse.PlaceInfo>> RequestPlacesAsync(int pageNumber, int pageSize, bool cleanCachedPages, CancellationToken ct)
    {
        var createNewPointer = false;
        if (placesPagePointer == null)
        {
            createNewPointer = true;
        }
        else if (cleanCachedPages)
        {
            placesPagePointer.Dispose();
            createNewPointer = true;
        }

        if (createNewPointer)
        {
            placesPagePointer = new LambdaResponsePagePointer<PlacesResponse>(
                PAGINATED_PLACES_END_POINT,
                pageSize, ct, this);
        }

        var pageResponse = await placesPagePointer.GetPageAsync(pageNumber, ct);

        if (!pageResponse.success)
            throw new Exception($"The request of the places failed!");

        var places = pageResponse.response.data;

        return places;
    }

    public void AddPlacesToCatalog(BaseDictionary<string, HotScenesController.PlaceInfo> placeItems)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    UniTask<(PlacesResponse response, bool success)> ILambdaServiceConsumer<PlacesResponse>.CreateRequest(string endPoint, int pageSize, int pageNumber, CancellationToken cancellationToken) =>
        lambdasService.Ref.Get<PlacesResponse>(
            END_POINT,
            endPoint,
            TIMEOUT,
            ATTEMPTS_NUMBER,
            cancellationToken,
            LambdaPaginatedResponseHelper.GetPageSizeParam(pageSize), LambdaPaginatedResponseHelper.GetPageNumParam(pageNumber));
}
