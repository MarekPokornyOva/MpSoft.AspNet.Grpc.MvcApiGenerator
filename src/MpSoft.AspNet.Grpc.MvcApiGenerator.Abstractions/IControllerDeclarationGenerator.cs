#region using
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.CodeDom;
#endregion using

namespace MpSoft.AspNet.Grpc.MvcApiGenerator
{
	public interface IControllerDeclarationGenerator
	{
		CodeNamespace Generate(ControllerDeclarationGeneratorContext context);
	}

	public readonly struct ControllerDeclarationGeneratorContext
	{
		public ControllerDeclarationGeneratorContext(ClassDeclarationSyntax grpcService,SemanticModel semanticModel)
		{
			GrpcService=grpcService;
			SemanticModel=semanticModel;
		}

		public ClassDeclarationSyntax GrpcService { get; }
		public SemanticModel SemanticModel { get; }
	}
}
