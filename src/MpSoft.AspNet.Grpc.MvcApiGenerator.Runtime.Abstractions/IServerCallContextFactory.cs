#region using
using Grpc.Core;
using Microsoft.AspNetCore.Http;
#endregion using

namespace MpSoft.AspNet.Grpc.MvcApiGenerator.Runtime
{
	public interface IServerCallContextFactory
	{
		ServerCallContext Create(HttpContext httpContext);
	}
}
