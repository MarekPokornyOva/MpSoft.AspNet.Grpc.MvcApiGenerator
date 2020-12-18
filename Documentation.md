# Installation

### Sample 01-Simple
Demoes the simplest implementation.

1. create new gRPC service project
2. add MpSoft.AspNet.Grpc.MvcApiGenerator and MpSoft.AspNet.Grpc.MvcApiGenerator.Runtime nuget packages
3. the legacy client applications might be able to communicate only via HTTP 1.1. To configure the application to use both HTTP 1.1 and HTTP/2, set Kernel endpoint defaults (optional)
```
  "Kestrel": {
    "EndpointDefaults": {
      "Protocols":  "Http1AndHttp2"
    }
  }
```
4. add standard MVC controllers' services
```
	services.AddControllers();
```
5. add gRPC controllers generator
```
	services.AddControllers()
		.AddGrpcHttp();
```
6. add gRPC services which should be called by REST
```
	services.AddControllers()
		.AddGrpcHttp();
	services.AddSingleton<GreeterService>();
```
7. decorate the gRPC services with both `[Mvcapi]` and `[MvcApiMethod]` attributes
```
	[MvcApi(BaseType = typeof(Microsoft.AspNetCore.Mvc.ControllerBase))]
	[MvcApiMethod(nameof(SayHello),HttpMethod = typeof(Microsoft.AspNetCore.Mvc.HttpPutAttribute),RouteTemplate = "api/"+nameof(GreeterService)+"/"+nameof(SayHello))]
```
8. add standard Swagger generator (optional)
```
	services.AddSwaggerGen(c =>
	{
		c.SwaggerDoc("v1",new OpenApiInfo { Title="gRPC HTTP API Example",Version="v1" });
	});
```
and
```
	app.UseSwagger();
	app.UseSwaggerUI(c =>
	{
		c.SwaggerEndpoint("/swagger/v1/swagger.json","gRPC HTTP API Example V1");
	});
```
9. add controllers' endpoints
```
	app.UseEndpoints(endpoints =>
	{
		endpoints.MapGrpcService<GreeterService>();
		endpoints.MapControllers();
	});
```

### Sample 02-Advanced
Demoes custom generation using plugin/extension. In order to launch the demo, you need to build 02-Advanced.Plugin project first to get built assembly, which will be used by MsBuild afterwards.
MsBuild will then be able to trigger the generator which will use the built assembly to enhance the generated controller code.

10. create .NET Standard project and ensure it's based on .NET Standard 2.0
11. create boot class
```
	static class GeneratorPluginBoot
	{
		public static IServiceProvider Setup(IServiceProvider serviceProvider)
			=> new PluginServiceProvider(serviceProvider);
	}
```
Ensure the class contains static method, has one parameter of IServiceProvider type and returns IServiceProvider.

12. the returned IServiceProvider will be used to provide necessary services.
13. create required services implementing following interfaces:
	- `IGrpcServiceLocator`: locates gRPC services within the project
    - `IControllerDeclarationGenerator`: transforms the gRPC services syntax nodes to controllers code
    - `IControllerCodeGenerator`: generates final code
    - `IControllerSourceTextGenerator`: transforms the final code to the form acceptable to MsBuild compilation
14. configure the plugin to be used during analyze/build:
    - create `Directory.Build.props` file within your project or solution directory
    - put your assembly path into
```
		<Project>
		  <PropertyGroup>
		    <MvcApiGenerator_BootAssemblyPath>..\02-Advanced.Plugin\bin\Debug\netstandard2.0\02-Advanced.Plugin.dll</MvcApiGenerator_BootAssemblyPath>
		  </PropertyGroup>
		  <ItemGroup>
			<CompilerVisibleProperty Include="MvcApiGenerator_BootAssemblyPath" />
		  </ItemGroup>
		</Project>
```

See the samples' code for detail understanding of the usage and configuration.
