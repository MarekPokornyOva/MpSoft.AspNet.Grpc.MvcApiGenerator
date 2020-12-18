#region using
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.CodeDom;
using System.Collections.Generic;
#endregion using

namespace MpSoft.AspNet.Grpc.MvcApiGenerator
{
	[Generator]
	public class MvcApiGenerator:ISourceGenerator
	{
		//https://github.com/dotnet/roslyn/blob/master/docs/features/source-generators.cookbook.md
		//https://cloud.google.com/endpoints/docs/grpc/transcoding
		public void Execute(GeneratorExecutionContext context)
		{
			//context.ReportDiagnostic(Diagnostic.Create("MvcApi-01","","I'm alive - Execute - diag",DiagnosticSeverity.Error,DiagnosticSeverity.Error,true,0));
			//context.ReportDiagnostic(Diagnostic.Create("MvcApi-01","","Generated 1",DiagnosticSeverity.Info,DiagnosticSeverity.Info,true,1));

			string language = context.Compilation.Language;
			IServiceProvider serviceProvider = new SimpleServiceProvider(
				context,
				new DefaultGrpcServiceLocator(),
				new DefaultControllerDeclarationGenerator(),
				new DefaultControllerCodeGenerator(language),
				new DefaultControllerSourceTextGenerator(language));

			if (context.AnalyzerConfigOptions.GlobalOptions.TryGetOptionsValue("BootAssemblyPath",out string bootAssemblyPath)&&(!string.IsNullOrEmpty(bootAssemblyPath)))
				serviceProvider=ServiceProviderExtender.TryExtend(bootAssemblyPath,serviceProvider);

			IGrpcServiceLocator grpcServiceLocator = null;
			IControllerDeclarationGenerator controllerDescriptorGenerator = null;
			IControllerCodeGenerator controllerCodeGenerator = null;
			IControllerSourceTextGenerator controllerSourceTextGenerator = null;

			try
			{
				grpcServiceLocator=serviceProvider.GetRequiredService<IGrpcServiceLocator>();
				controllerDescriptorGenerator=serviceProvider.GetRequiredService<IControllerDeclarationGenerator>();
				controllerCodeGenerator=serviceProvider.GetRequiredService<IControllerCodeGenerator>();
				controllerSourceTextGenerator=serviceProvider.GetRequiredService<IControllerSourceTextGenerator>();

				IEnumerable<ClassDeclarationSyntax> grpcServices = grpcServiceLocator.Locate(context);
				foreach (ClassDeclarationSyntax grpcService in grpcServices)
				{
					SemanticModel semanticModel = context.Compilation.GetSemanticModel(grpcService.SyntaxTree);
					CodeNamespace @namespace = controllerDescriptorGenerator.Generate(new ControllerDeclarationGeneratorContext(grpcService,semanticModel));
					string code = controllerCodeGenerator.Generate(new ControllerCodeGeneratorContext(@namespace));
					(string filenameHint, SourceText sourceText)=controllerSourceTextGenerator.Generate(new ControllerSourceTextGeneratorContext(@namespace,code));
					context.AddSource(filenameHint,sourceText);
				}
			}
			finally
			{
				DisposeService(controllerSourceTextGenerator);
				DisposeService(controllerCodeGenerator);
				DisposeService(controllerDescriptorGenerator);
				DisposeService(grpcServiceLocator);
				DisposeService(serviceProvider);
			}
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		static void DisposeService(object service)
		{
			if (service is IDisposable dis)
				dis.Dispose();
		}

		public void Initialize(GeneratorInitializationContext context)
		{
		}
	}

	static class AnalyzerConfigOptionsExtensions
	{
		internal static bool TryGetOptionsValue(this AnalyzerConfigOptions options,string key,out string value)
			=> options.TryGetValue("MvcApiGenerator_"+key,out value)||options.TryGetValue("build_property.MvcApiGenerator_"+key,out value);
	}

	static class ServiceProviderServiceExtensions
	{
		internal static T GetRequiredService<T>(this IServiceProvider provider)
		{
			if (provider==null)
				throw new ArgumentNullException("provider");
			Type serviceType = typeof(T);
			object res = provider.GetService(typeof(T));
			if (res==null)
				throw new InvalidOperationException($"No {serviceType} registered.");
			return (T)res;
		}
	}
}
