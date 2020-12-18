#region using
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MpSoft.AspNet.Grpc.MvcApiGenerator;
using System;
using System.CodeDom;
#endregion using

namespace Advanced.Plugin
{
	class PluginControllerDeclarationGenerator:IControllerDeclarationGenerator
	{
		readonly IControllerDeclarationGenerator _inner;

		internal PluginControllerDeclarationGenerator(IControllerDeclarationGenerator inner)
			=> _inner=inner;

		public CodeNamespace Generate(ControllerDeclarationGeneratorContext context)
		{
			CodeNamespace result = _inner.Generate(context);

			foreach (CodeTypeMember member in result.Types[0].Members)
				if ((member is CodeMemberMethod method) && (member is not CodeConstructor))
					ModifyMethod(method,string.Equals(context.SemanticModel.Compilation.Language,"c#",StringComparison.OrdinalIgnoreCase));

			return result;
		}

		static void ModifyMethod(CodeMemberMethod method,bool makeAsync)
		{
			//Replace [HttpPut(...)] attribute by [HttpGet(...)]
			CodeAttributeDeclaration attr = method.CustomAttributes[0];
			attr.AttributeType.BaseType=attr.Name="Microsoft.AspNetCore.Mvc.HttpGetAttribute";

			//Add [FromQuery] attribute to the first parameter
			method.Parameters[0].CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("Microsoft.AspNetCore.Mvc.FromQueryAttribute")));

			//Make the method async
			method.ReturnType.BaseType="async "+method.ReturnType.BaseType;

			CodeFieldReferenceExpression returnExpr = (CodeFieldReferenceExpression)((CodeMethodInvokeExpression)((CodeMethodReturnStatement)method.Statements[0]).Expression).Method.TargetObject;
			returnExpr.TargetObject=new CodeSnippetExpression("await this");
		}
	}
}