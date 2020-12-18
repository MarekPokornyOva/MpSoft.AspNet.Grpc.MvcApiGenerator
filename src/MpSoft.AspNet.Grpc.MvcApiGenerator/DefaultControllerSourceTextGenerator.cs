#region using
using Microsoft.CodeAnalysis.Text;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;
#endregion using

namespace MpSoft.AspNet.Grpc.MvcApiGenerator
{
	sealed class DefaultControllerSourceTextGenerator:IControllerSourceTextGenerator
	{
		readonly string _fileExtension;

		public DefaultControllerSourceTextGenerator(string language)
		{
			using (CodeDomProvider generator = CodeDomProvider.CreateProvider(language))
				_fileExtension=generator.FileExtension;
		}

		public (string FilenameHint, SourceText SourceText) Generate(ControllerSourceTextGeneratorContext context)
		{
			CodeNamespace @namespace = context.Namespace;
			string filename=@namespace.Types.Count==0
				? @namespace.Name
				: @namespace.Types[0].Name;
			if (string.IsNullOrEmpty(filename))
				filename=Guid.NewGuid().ToString("N");
			return (Path.ChangeExtension(filename,_fileExtension), SourceText.From(context.Code,Encoding.UTF8));
		}
	}
}