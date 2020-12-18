#region using
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
#endregion using

namespace MpSoft.AspNet.Grpc.MvcApiGenerator
{
	public interface IGrpcServiceLocator
	{
		IEnumerable<ClassDeclarationSyntax> Locate(GeneratorExecutionContext context);
	}
}
