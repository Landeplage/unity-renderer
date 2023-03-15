using Cysharp.Threading.Tasks;
using DCL;
using System.Threading;

namespace DCLServices.Lambdas.PlaceService
{
    public class PlacesService : IPlacesService, ILambdaServiceConsumer<PlacesResponse>
    {
        internal const string END_POINT = "api/places?order_by=most_active&order=desc&with_realms_detail=true";
        internal const int TIMEOUT = ILambdasService.DEFAULT_TIMEOUT;
        internal const int ATTEMPTS_NUMBER = ILambdasService.DEFAULT_ATTEMPTS_NUMBER;

        private Service<ILambdasService> lambdasService;

        public LambdaResponsePagePointer<PlacesResponse> GetPaginationPointer(int pageSize, CancellationToken ct) =>
            new (END_POINT, pageSize, ct, this);

        UniTask<(PlacesResponse response, bool success)> ILambdaServiceConsumer<PlacesResponse>.CreateRequest(string endPoint, int pageSize, int pageNumber, CancellationToken cancellationToken) =>
            lambdasService.Ref.Get<PlacesResponse>(
                END_POINT,
                endPoint,
                TIMEOUT,
                ATTEMPTS_NUMBER,
                cancellationToken,
                LambdaPaginatedResponseHelper.GetPageSizeParam(pageSize), LambdaPaginatedResponseHelper.GetPageNumParam(pageNumber));

        public void Dispose() { }

        public void Initialize() { }
    }
}
