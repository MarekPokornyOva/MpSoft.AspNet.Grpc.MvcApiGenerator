#region using
using MpSoft.AspNet.Grpc.MvcApiGenerator;
using System;
#endregion using

namespace Advanced.Plugin
{
	class PluginServiceProvider:IServiceProvider
	{
		readonly IServiceProvider _inner;

		public PluginServiceProvider(IServiceProvider inner)
			=> _inner=inner;

		public object GetService(Type serviceType)
		{
			object result = _inner.GetService(serviceType);
			if (serviceType==typeof(IControllerDeclarationGenerator))
				result=new PluginControllerDeclarationGenerator((IControllerDeclarationGenerator)result);
			return result;
		}
	}
}