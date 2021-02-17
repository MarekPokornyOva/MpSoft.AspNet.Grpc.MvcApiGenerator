#region using
using Grpc.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MpSoft.AspNet.Grpc.MvcApiGenerator.Runtime;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#endregion using

namespace MpSoft.AspNet.Grpc.MvcApiGenerator
{
	public class DefaultControllerDeclarationGenerator:IControllerDeclarationGenerator
	{
		readonly static string _assemblyName = typeof(MvcApiAttribute).Assembly.GetName().Name;
		readonly static string _mvcApiAttributeFullName = typeof(MvcApiAttribute).FullName;
		readonly static string _mvcApiMethodAttributeFullName = typeof(MvcApiMethodAttribute).FullName;
		readonly static string _authorizeAttributeAssemblyName = "Microsoft.AspNetCore.Authorization";
		readonly static string _authorizeAttributeFullName = "Microsoft.AspNetCore.Authorization.AuthorizeAttribute";
		readonly static string _allowAnonymousAttributeFullName = "Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute";
		readonly internal static Func<TypeInfo,bool> EqualsMvcApiAttribute = ti => ti.ConvertedType.Equals(_mvcApiAttributeFullName,_assemblyName);
		readonly internal static Func<TypeInfo,bool> EqualsMvcApiMethodAttribute = ti => ti.ConvertedType.Equals(_mvcApiMethodAttributeFullName,_assemblyName);
		readonly internal static Func<TypeInfo,bool> EqualsAuthorizeAttribute = ti => ti.ConvertedType.Equals(_authorizeAttributeFullName,_authorizeAttributeAssemblyName);
		readonly internal static Func<TypeInfo,bool> EqualsAllowAnonymousAttribute = ti => ti.ConvertedType.Equals(_allowAnonymousAttributeFullName,_authorizeAttributeAssemblyName);

		public virtual CodeNamespace Generate(ControllerDeclarationGeneratorContext context)
		{
			ClassDeclarationSyntax grpcService = context.GrpcService;
			SemanticModel semanticModel = context.SemanticModel;

			var syntaxTree = grpcService.SyntaxTree;
			PreprocessedAttributeSyntax[] attrs = grpcService.AttributeLists.SelectMany(x => x.Attributes).Select(x=>x.Preprocess(semanticModel)).ToArray();
			PreprocessedAttributeSyntax mvcApiAttribute = attrs.FirstOrDefault(x => EqualsMvcApiAttribute(x.TypeInfo));

			string typeName = null, typeNamespace = null;
			if (mvcApiAttribute!=default)
			{
				typeName=mvcApiAttribute.Arguments.FindByName(nameof(MvcApiAttribute.Name))?.GetConstantValueString(null);
				typeNamespace=mvcApiAttribute.Arguments.FindByName(nameof(MvcApiAttribute.Namespace))?.GetConstantValueString(null);
			}

			CodeTypeDeclaration typeDeclaration = new CodeTypeDeclaration(typeName??grpcService.Identifier.ToString()+"Controller") { IsPartial=true };

			CodeTypeReference serviceType = new CodeTypeReference(GetFullName(grpcService));
			CodeTypeReference ctxFactType = new CodeTypeReference(typeof(IServerCallContextFactory));
			typeDeclaration.Members.Add(new CodeMemberField(serviceType,"_service"));
			typeDeclaration.Members.Add(new CodeMemberField(ctxFactType,"_ctxFact"));
			typeDeclaration.Members.Add(GenerateConstructor(serviceType,ctxFactType));

			//base type
			if (mvcApiAttribute!=default)
			{
				PreprocessedAttributeArgument aas = mvcApiAttribute.Arguments.FindByName(nameof(MvcApiAttribute.BaseType));
				if (aas!=default)
					typeDeclaration.BaseTypes.Add(aas.GetTypeTypeInfo().ToCodeTypeReference());
			}
			typeDeclaration.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("Microsoft.AspNetCore.Mvc.ApiControllerAttribute")));

			//transfer Authorize and AllowAnonymous attributes
			TransferAttributes(attrs,EqualsAuthorizeAttribute,typeDeclaration,_ => new CodeTypeReference(_authorizeAttributeFullName));
			TransferAttributes(attrs,EqualsAllowAnonymousAttribute,typeDeclaration,_ => new CodeTypeReference(_allowAnonymousAttributeFullName));

			PreprocessedAttributeSyntax[] mvcApiMethodAttributes = attrs.Where(x => EqualsMvcApiMethodAttribute(x.TypeInfo)).ToArray();
			typeDeclaration.Members.AddRange(grpcService.Members.OfType<MethodDeclarationSyntax>()
				.Select(m => 
				{
					string methodName = m.Identifier.ToString(); 
					return (Method: m, Attribute: mvcApiMethodAttributes.FirstOrDefault(attr =>
					{
						Optional<object> val = attr.Arguments[0].GetConstantValue();
						return val.HasValue && string.Equals(val.Value as string,methodName,StringComparison.Ordinal);
					}));
				})
				.Where(x => x.Attribute!=default)
				.Select(m => GenerateMethodDeclaration(m.Method,semanticModel,m.Attribute)).ToArray());

			CodeNamespace ns = new CodeNamespace(typeNamespace);
			ns.Types.Add(typeDeclaration);
			return ns;
		}

		static CodeMemberMethod GenerateMethodDeclaration(MethodDeclarationSyntax methodDeclarationSyntax,SemanticModel semanticModel,PreprocessedAttributeSyntax methodConfig)
		{
			string name = methodDeclarationSyntax.Identifier.ToString();
			CodeMemberMethod methodDeclaration = new CodeMemberMethod()
			{
				Name=name,
				Attributes=MemberAttributes.Public,
				ReturnType=semanticModel.GetTypeInfo(methodDeclarationSyntax.ReturnType).ToCodeTypeReference()
			};
			TypeInfo? httpMethod = methodConfig.Arguments.FindByName("HttpMethod")?.GetTypeTypeInfo();
			Optional<object>? routeTemplate = methodConfig.Arguments.FindByName("RouteTemplate")?.GetConstantValue();
			methodDeclaration.CustomAttributes.Add(new CodeAttributeDeclaration(
				httpMethod.HasValue ? httpMethod.Value.ToCodeTypeReference() : new CodeTypeReference("Microsoft.AspNetCore.Mvc.HttpPutAttribute"),
				new CodeAttributeArgument(new CodePrimitiveExpression(routeTemplate.HasValue&&routeTemplate.Value.HasValue ? routeTemplate.Value.Value : name))
				));

			(ParameterSyntax ParameterSyntax, string Name, ITypeSymbol Type, bool IsCtx)[] parameterInfos = methodDeclarationSyntax.ParameterList.Parameters
				.Select(parameterSyntax => { ITypeSymbol convType = semanticModel.GetTypeInfo(parameterSyntax.Type).ConvertedType; return (parameterSyntax, parameterSyntax.Identifier.ToString(), convType, convType.Equals("Grpc.Core.ServerCallContext","Grpc.Core.Api")); })
				.ToArray();
			methodDeclaration.Parameters.AddRange(parameterInfos.Where(x=>!x.IsCtx).Select(p => GenerateParameterDeclaration(p.Name,p.Type)).ToArray());


			//transfer Authorize and AllowAnonymous attributes
			PreprocessedAttributeSyntax[] attrs = methodDeclarationSyntax.AttributeLists.SelectMany(x => x.Attributes).Select(x => x.Preprocess(semanticModel)).ToArray();
			TransferAttributes(attrs,EqualsAuthorizeAttribute,methodDeclaration,_ => new CodeTypeReference(_authorizeAttributeFullName));
			TransferAttributes(attrs,EqualsAllowAnonymousAttribute,methodDeclaration,_ => new CodeTypeReference(_allowAnonymousAttributeFullName));

			//method source code
			//return _service.SayHello(request, _ctxFact.Create(base.HttpContext));
			CodeExpression[] parms = parameterInfos.Select(parmInfo =>
				parmInfo.IsCtx
					//_ctxFact.Create(base.HttpContext)
					? (CodeExpression)new CodeMethodInvokeExpression(
										new CodeFieldReferenceExpression(
											new CodeThisReferenceExpression(),
											"_ctxFact"
										),
										nameof(IServerCallContextFactory.Create),
										new CodePropertyReferenceExpression(new CodeBaseReferenceExpression(),"HttpContext")
									)
					//request (=argument)
					: new CodeArgumentReferenceExpression(parmInfo.Name)
			).ToArray();
			methodDeclaration.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),"_service"),name,parms)));

			return methodDeclaration;
		}

		static CodeParameterDeclarationExpression GenerateParameterDeclaration(string name, ITypeSymbol type)
			=> new CodeParameterDeclarationExpression
			{
				Name=name,
				Type=type.ToCodeTypeReference()
			};

		static CodeConstructor GenerateConstructor(CodeTypeReference serviceType,CodeTypeReference ctxFactType)
		{
			CodeConstructor ctorDeclaration = new CodeConstructor() { Attributes=MemberAttributes.Public | MemberAttributes.Final };
			ctorDeclaration.Parameters.Add(new CodeParameterDeclarationExpression(serviceType,"service"));
			ctorDeclaration.Parameters.Add(new CodeParameterDeclarationExpression(ctxFactType,"ctxFact"));

			ctorDeclaration.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),"_service"),new CodeArgumentReferenceExpression(ctorDeclaration.Parameters[0].Name)));
			ctorDeclaration.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),"_ctxFact"),new CodeArgumentReferenceExpression(ctorDeclaration.Parameters[1].Name)));

			return ctorDeclaration;
		}

		static string GetFullName(BaseTypeDeclarationSyntax typeSyntax)
		{
			StringBuilder res = new StringBuilder(typeSyntax.Identifier.ToString());
			SyntaxNode parent = typeSyntax;
			while ((parent=parent.Parent)!=null)
			{
				string part = parent switch
				{
					BaseTypeDeclarationSyntax btds => btds.Identifier.ToString(),
					NamespaceDeclarationSyntax nds => nds.Name.ToString(),
					_ => null
				};
				if (part!=null)
				{
					res.Insert(0,'.');
					res.Insert(0,part);
				}
			}
			return res.ToString();
		}

		static void TransferAttributes(IEnumerable<PreprocessedAttributeSyntax> attrs,Func<TypeInfo,bool> attrFilter,CodeTypeMember member,Func<PreprocessedAttributeSyntax,CodeTypeReference> attrTypeProvider)
		{
			foreach (PreprocessedAttributeSyntax attr in attrs)
				if (attrFilter(attr.TypeInfo))
				{
					CodeAttributeArgument[] newArgs = attr.Arguments.Select(x =>
					{
						Optional<object> val = x.GetConstantValue();
						return new CodeAttributeArgument(x.Name,new CodePrimitiveExpression(val.HasValue ? val.Value : null));
					}).ToArray();
					member.CustomAttributes.Add(new CodeAttributeDeclaration(attrTypeProvider(attr),newArgs));
				}
		}
	}

	static class TypeSymbolHelpers
	{
		internal static CodeTypeReference ToCodeTypeReference(this TypeInfo typeInfo)
			=> typeInfo.ConvertedType.ToCodeTypeReference();

		internal static CodeTypeReference ToCodeTypeReference(this ITypeSymbol typeSymbol)
			=> new CodeTypeReference(typeSymbol.ToString());

		internal static bool Equals(this ITypeSymbol typeSymbol,string fullname,string assemblyName)
			=> string.Equals(typeSymbol.ToString(),fullname,StringComparison.Ordinal)&&string.Equals(typeSymbol.ContainingAssembly.Name,assemblyName,StringComparison.Ordinal);

		internal static PreprocessedAttributeSyntax Preprocess(this AttributeSyntax attributeSyntax,SemanticModel semanticModel)
			=> new PreprocessedAttributeSyntax(attributeSyntax,semanticModel);

		internal static PreprocessedAttributeArgument FindByName(this PreprocessedAttributeArgument[] attributeArguments,string name)
			=> attributeArguments.FirstOrDefault(attr => string.Equals(attr.Name,name,StringComparison.Ordinal));

		internal static string GetConstantValueString(this PreprocessedAttributeArgument argument,string defaultValue)
		{
			Optional<object> val = argument.GetConstantValue();
			return val.HasValue
				? val.Value as string
				: defaultValue;
		}
	}

	class PreprocessedAttributeSyntax
	{
		internal PreprocessedAttributeSyntax(AttributeSyntax syntax,SemanticModel semanticModel)
		{
			Syntax=syntax;
			TypeInfo=semanticModel.GetTypeInfo(syntax);
			Arguments=syntax.ArgumentList==null ? new PreprocessedAttributeArgument[0] : syntax.ArgumentList.Arguments.Select(x => new PreprocessedAttributeArgument(x,semanticModel)).ToArray();
		}

		internal AttributeSyntax Syntax { get; }
		internal TypeInfo TypeInfo { get; }
		internal PreprocessedAttributeArgument[] Arguments { get; }
	}

	class PreprocessedAttributeArgument
	{
		readonly SemanticModel _semanticModel;

		public PreprocessedAttributeArgument(AttributeArgumentSyntax syntax,SemanticModel semanticModel)
		{
			Syntax=syntax;
			_semanticModel=semanticModel;
			Name=syntax.NameEquals?.Name?.ToString();
		}

		internal AttributeArgumentSyntax Syntax { get; }
		internal string Name { get; }
		internal Optional<object> GetConstantValue() => _semanticModel.GetConstantValue(Syntax.Expression);
		internal TypeInfo GetTypeInfo() => _semanticModel.GetTypeInfo(Syntax.Expression);
		internal TypeSyntax GetTypeSyntax() => ((TypeOfExpressionSyntax)Syntax.Expression).Type;
		internal TypeInfo GetTypeTypeInfo() => _semanticModel.GetTypeInfo(GetTypeSyntax());
	}
}