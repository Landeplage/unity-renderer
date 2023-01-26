// AUTOGENERATED, DO NOT EDIT
// Type definitions for server implementations of ports.
// package: decentraland.renderer.renderer_services
// file: decentraland/renderer/renderer_services/friend_request_renderer.proto
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using rpc_csharp.protocol;
using rpc_csharp;
namespace Decentraland.Renderer.RendererServices {
public interface IFriendRequestRendererService<Context>
{

  UniTask<ApproveFriendRequestReply> ApproveFriendRequest(ApproveFriendRequestPayload request, Context context, CancellationToken ct);

  UniTask<RejectFriendRequestReply> RejectFriendRequest(RejectFriendRequestPayload request, Context context, CancellationToken ct);

  UniTask<CancelFriendRequestReply> CancelFriendRequest(CancelFriendRequestPayload request, Context context, CancellationToken ct);

  UniTask<ReceiveFriendRequestReply> ReceiveFriendRequest(ReceiveFriendRequestPayload request, Context context, CancellationToken ct);

}

public static class FriendRequestRendererServiceCodeGen
{
  public const string ServiceName = "FriendRequestRendererService";

  public static void RegisterService<Context>(RpcServerPort<Context> port, IFriendRequestRendererService<Context> service)
  {
    var result = new ServerModuleDefinition<Context>();

    result.definition.Add("ApproveFriendRequest", async (payload, context, ct) => { var res = await service.ApproveFriendRequest(ApproveFriendRequestPayload.Parser.ParseFrom(payload), context, ct); return res?.ToByteString(); });
    result.definition.Add("RejectFriendRequest", async (payload, context, ct) => { var res = await service.RejectFriendRequest(RejectFriendRequestPayload.Parser.ParseFrom(payload), context, ct); return res?.ToByteString(); });
    result.definition.Add("CancelFriendRequest", async (payload, context, ct) => { var res = await service.CancelFriendRequest(CancelFriendRequestPayload.Parser.ParseFrom(payload), context, ct); return res?.ToByteString(); });
    result.definition.Add("ReceiveFriendRequest", async (payload, context, ct) => { var res = await service.ReceiveFriendRequest(ReceiveFriendRequestPayload.Parser.ParseFrom(payload), context, ct); return res?.ToByteString(); });

    port.RegisterModule(ServiceName, (port) => UniTask.FromResult(result));
  }
}
}
