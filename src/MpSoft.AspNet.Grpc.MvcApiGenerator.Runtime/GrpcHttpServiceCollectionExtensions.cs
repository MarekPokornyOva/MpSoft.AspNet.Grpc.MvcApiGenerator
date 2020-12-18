#region using
using MpSoft.AspNet.Grpc.MvcApiGenerator.Runtime;
#endregion using

namespace Microsoft.Extensions.DependencyInjection
{
	public static class GrpcHttpServiceCollectionExtensions
	{
		public static IMvcBuilder AddGrpcHttp(this IMvcBuilder mvcCoreBuilder)
		{
			mvcCoreBuilder.Services.AddSingleton<IServerCallContextFactory,DefaultServerCallContextFactory>();
			return mvcCoreBuilder;
		}
	}
}
