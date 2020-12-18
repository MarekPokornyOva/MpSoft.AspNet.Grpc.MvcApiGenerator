#region using
using Microsoft.CodeAnalysis;
using System;
#endregion using

namespace MpSoft.AspNet.Grpc.MvcApiGenerator
{
	class SimpleServiceProvider:IServiceProvider
	{
		readonly GeneratorExecutionContext _context;
		readonly IGrpcServiceLocator _grpcServiceLocator;
		readonly IControllerDeclarationGenerator _controllerDescriptorGenerator;
		readonly IControllerCodeGenerator _controllerCodeGenerator;
		readonly IControllerSourceTextGenerator _controllerSourceTextGenerator;

		internal SimpleServiceProvider(GeneratorExecutionContext context,
			IGrpcServiceLocator grpcServiceLocator,
			IControllerDeclarationGenerator controllerDescriptorGenerator,
			IControllerCodeGenerator controllerCodeGenerator,
			IControllerSourceTextGenerator controllerSourceTextGenerator)
		{
			_context=context;
			_grpcServiceLocator=grpcServiceLocator;
			_controllerDescriptorGenerator=controllerDescriptorGenerator;
			_controllerCodeGenerator=controllerCodeGenerator;
			_controllerSourceTextGenerator=controllerSourceTextGenerator;
		}

		public object GetService(Type serviceType)
			=> serviceType==typeof(GeneratorExecutionContext) ? _context :
				serviceType==typeof(IGrpcServiceLocator) ? _grpcServiceLocator :
				serviceType==typeof(IControllerDeclarationGenerator) ? _controllerDescriptorGenerator :
				serviceType==typeof(IControllerCodeGenerator) ? _controllerCodeGenerator :
				serviceType==typeof(IControllerSourceTextGenerator) ? _controllerSourceTextGenerator :
				(object)null;
	}
}
