#region using
using System;
using System.CodeDom.Compiler;
using System.IO;
#endregion using

namespace MpSoft.AspNet.Grpc.MvcApiGenerator
{
	public class DefaultControllerCodeGenerator:IControllerCodeGenerator,IDisposable
	{
		readonly CodeDomProvider _generator;
		readonly CodeGeneratorOptions _codeGeneratorOptions = new CodeGeneratorOptions() { IndentString="\t" };

		public DefaultControllerCodeGenerator(string language)
			=> _generator = CodeDomProvider.CreateProvider(language);

		public void Dispose()
			=> _generator.Dispose();

		public string Generate(ControllerCodeGeneratorContext context)
		{
			StringWriter sw = new StringWriter();
			_generator.GenerateCodeFromNamespace(context.Namespace,sw,_codeGeneratorOptions);
			return sw.GetStringBuilder().ToString();
		}
	}
}