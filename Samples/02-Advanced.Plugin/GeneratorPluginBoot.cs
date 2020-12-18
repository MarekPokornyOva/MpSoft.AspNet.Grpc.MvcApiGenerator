#region using
using System;
#endregion using

namespace Advanced.Plugin
{
	static class GeneratorPluginBoot
	{
		public static IServiceProvider Setup(IServiceProvider serviceProvider)
			=> new PluginServiceProvider(serviceProvider);
	}
}
