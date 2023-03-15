using DCL;
using System.Threading;

namespace DCLServices.Lambdas.PlaceService
{
    public interface IPlacesService : IService
    {
        LambdaResponsePagePointer<PlacesResponse> GetPaginationPointer(int pageSize, CancellationToken ct);
    }
}
