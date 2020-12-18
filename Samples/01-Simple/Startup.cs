using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Simple
{
	public class Startup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddGrpc();
			services.AddControllers()
				.AddGrpcHttp();
			services.AddSingleton<GreeterService>();

			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1",new OpenApiInfo { Title="gRPC HTTP API Example",Version="v1" });
			});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app,IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseDefaultFiles();
			app.UseStaticFiles();

			app.UseSwagger();
			app.UseSwaggerUI(c =>
			{
				c.SwaggerEndpoint("/swagger/v1/swagger.json","gRPC HTTP API Example V1");
			});

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapGrpcService<GreeterService>();
				endpoints.MapControllers();
				endpoints.MapGet("/",async context =>
				  {
					 await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
				 });
			});
		}
	}
}
