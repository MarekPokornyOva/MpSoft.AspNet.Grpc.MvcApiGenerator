#region using
using System;
using System.Reflection;
#endregion using

namespace MpSoft.AspNet.Grpc.MvcApiGenerator
{
	static class ServiceProviderExtender
	{
		internal static IServiceProvider TryExtend(string bootAssemblyPath,IServiceProvider serviceProvider)
		{
			MethodInfo miConfigure = FindConfigureMethod(Assembly.LoadFrom(bootAssemblyPath));
			return miConfigure==null
				? serviceProvider
				: (IServiceProvider)miConfigure.Invoke(null,new object[] { serviceProvider });
		}

		readonly static Type _serviceProviderType = typeof(IServiceProvider);
		static MethodInfo FindConfigureMethod(Assembly asm)
		{
			foreach (Type type in asm.GetTypes())
				foreach (MethodInfo mi in type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static))
					if (mi.ReturnType==_serviceProviderType)
					{
						ParameterInfo[] pis = mi.GetParameters();
						if ((pis.Length==1)&&pis[0].ParameterType==_serviceProviderType)
							return mi;
					}
			return null;
		}
	}
}
