#region using
using Microsoft.CodeAnalysis.Text;
using System.CodeDom;
#endregion using

namespace MpSoft.AspNet.Grpc.MvcApiGenerator
{
	public interface IControllerSourceTextGenerator
	{
		(string FilenameHint, SourceText SourceText) Generate(ControllerSourceTextGeneratorContext context);
	}

	public readonly struct ControllerSourceTextGeneratorContext
	{
		public ControllerSourceTextGeneratorContext(CodeNamespace @namespace,string code)
		{
			Namespace=@namespace;
			Code=code;
		}

		public CodeNamespace Namespace { get; }
		public string Code { get; }
	}
}
