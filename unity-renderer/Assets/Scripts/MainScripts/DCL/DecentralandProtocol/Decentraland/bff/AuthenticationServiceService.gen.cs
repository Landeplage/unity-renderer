// AUTOGENERATED, DO NOT EDIT
// Type definitions for server implementations of ports.
// package: decentraland.bff
// file: decentraland/bff/authentication_service.proto
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using rpc_csharp.protocol;
using rpc_csharp;
using Google.Protobuf.WellKnownTypes;
namespace Decentraland.Bff {
public interface IBffAuthenticationService<Context>
{

  UniTask<GetChallengeResponse> GetChallenge(GetChallengeRequest request, Context context, CancellationToken ct);

  UniTask<WelcomePeerInformation> Authenticate(SignedChallenge request, Context context, CancellationToken ct);

  UniTask<DisconnectionMessage> GetDisconnectionMessage(Empty request, Context context, CancellationToken ct);

}

public static class BffAuthenticationServiceCodeGen
{
  public const string ServiceName = "BffAuthenticationService";

  public static void RegisterService<Context>(RpcServerPort<Context> port, IBffAuthenticationService<Context> service)
  {
    var result = new ServerModuleDefinition<Context>();
      
    result.definition.Add("GetChallenge", async (payload, context, ct) => { var res = await service.GetChallenge(GetChallengeRequest.Parser.ParseFrom(payload), context, ct); return res?.ToByteString(); });
    result.definition.Add("Authenticate", async (payload, context, ct) => { var res = await service.Authenticate(SignedChallenge.Parser.ParseFrom(payload), context, ct); return res?.ToByteString(); });
    result.definition.Add("GetDisconnectionMessage", async (payload, context, ct) => { var res = await service.GetDisconnectionMessage(Empty.Parser.ParseFrom(payload), context, ct); return res?.ToByteString(); });

    port.RegisterModule(ServiceName, (port) => UniTask.FromResult(result));
  }
}
}
