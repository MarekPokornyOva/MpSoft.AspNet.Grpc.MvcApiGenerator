#region using
using Grpc.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
#endregion using

namespace MpSoft.AspNet.Grpc.MvcApiGenerator
{
	sealed class DefaultGrpcServiceLocator:IGrpcServiceLocator
	{
		readonly static IEnumerable<ClassDeclarationSyntax> _emptyNodes = Enumerable.Empty<ClassDeclarationSyntax>();

		public IEnumerable<ClassDeclarationSyntax> Locate(GeneratorExecutionContext context)
		{
			ITypeSymbol mvcApiAttribute = context.Compilation.GetTypeByMetadataName(typeof(MvcApiAttribute).FullName);
			return context.Compilation.SyntaxTrees.SelectMany(syntaxTree => syntaxTree.TryGetRoot(out SyntaxNode rootNode) ? ExploreNode(rootNode,mvcApiAttribute,context.Compilation.GetSemanticModel(syntaxTree)) : _emptyNodes).ToArray();
		}

		static IEnumerable<ClassDeclarationSyntax> ExploreNode(SyntaxNode node,ITypeSymbol mvcApiAttribute,SemanticModel semanticModel)
		{
			IEnumerable<ClassDeclarationSyntax> result=null;
			if (node is ClassDeclarationSyntax cds)
			{
				if (cds.AttributeLists.SelectMany(list => list.Attributes)
						.Any(x=> DefaultControllerDeclarationGenerator.EqualsMvcApiAttribute(semanticModel.GetTypeInfo(x))))
					result=cds.AsEnumerable();
			}
			return (result??_emptyNodes).Concat(node.ChildNodes().SelectMany(childNode=>ExploreNode(childNode,mvcApiAttribute,semanticModel)));
		}
	}
}