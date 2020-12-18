#region using
using System;
#endregion using

namespace Grpc.Core
{
	[AttributeUsage(AttributeTargets.Class)]
	public class MvcApiAttribute:Attribute
	{
		public Type BaseType { get; set; }
		public string Name { get; set; }
		public string Namespace { get; set; }
		//public string HelperAssembly { get; set; }
	}

	[AttributeUsage(AttributeTargets.Class,AllowMultiple = true)]
	public class MvcApiMethodAttribute:Attribute
	{
		public MvcApiMethodAttribute(string name)
			=> Name=name;

		public string Name { get; }
		public Type HttpMethod { get; set; }
		public string RouteTemplate { get; set; }
	}
}
