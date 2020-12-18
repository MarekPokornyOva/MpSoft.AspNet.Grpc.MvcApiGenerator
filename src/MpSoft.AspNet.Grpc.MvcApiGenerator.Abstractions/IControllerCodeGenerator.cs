#region using
using System.CodeDom;
#endregion using

namespace MpSoft.AspNet.Grpc.MvcApiGenerator
{
	public interface IControllerCodeGenerator
	{
		string Generate(ControllerCodeGeneratorContext context);
	}

	public readonly struct ControllerCodeGeneratorContext
	{
		public ControllerCodeGeneratorContext(CodeNamespace @namespace)
		{
			Namespace=@namespace;
		}

		public CodeNamespace Namespace { get; }
	}
}
