using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Core.Common;

namespace Core.Api
{
    public static class Extensions
    {
		private const string Domain = "";
		private const string ApiDomain = "";

		/// <summary>
		/// Run the configuration of:
		///  - Fluent Validation
		///  - Swagger
		///  - CORS
		///  - HealthChecks
		///  - Logging
		///  - Kestrel Options
		///  - Fluent To Swagger
		/// </summary>
		/// <param name="serviceCollection">Service Collection</param>
		/// <param name="configuration">Current Application Configuration</param>
		/// <param name="version">Api Version</param>
		/// <param name="fv">Configuration Of FluentValidation</param>
		/// <returns>MvcBuilder</returns>
		public static IMvcBuilder SetupTcpWebApi(this IServiceCollection serviceCollection,
			IConfiguration configuration,
			string version = "v1",
			Action<FluentValidationMvcConfiguration> fv = null)
		{
			serviceCollection.AddOptions<WebApiOptions>()
				.Bind(configuration.GetSection("ApiOptions"));

			serviceCollection.AddKestrelServerOptions(configuration);
			serviceCollection.ConfigureSwagger(version);
			serviceCollection.AddCors();
			serviceCollection.SetupDefaultLogging(configuration);

			var mvcBuilder = serviceCollection.AddMvc()
					.SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

			return mvcBuilder;
		}

		/// <summary>
		/// Configure the application in Builder:
		///  - Swagger
		///  - CORS
		///  - HealthChecks
		///  - Logging
		///  - Routing
		/// </summary>
		/// <param name="app">Application Builder</param>
		/// <param name="env">Hosting Environment</param>
		public static void UseTcpWebApi(this IApplicationBuilder app, IWebHostEnvironment env)
		{
			app.UseMiddleware<AggregationIdMiddleware>();
			app.BuildSwagger();

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseHsts();
			}

			app.UseCors("AllowAll");
			app.UseRouting();
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}

		/// <summary>
		/// Add the default Cors of tcp apis with expose pagination headers
		/// </summary>
		/// <param name="services"></param>
		public static void AddCors(this IServiceCollection services)
		{
			services.AddCors(options => options.AddPolicy("AllowAll", p => p.AllowAnyOrigin()
				.AllowAnyMethod()
				.AllowAnyHeader()
				.WithExposedHeaders("Content-Range")
				.WithExposedHeaders("X-Total-Count")
				.WithExposedHeaders("Link")
			));
		}

		/// <summary>
		/// Add the maximum number of concurrent requests setting
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configuration"></param>
		public static void AddKestrelServerOptions(this IServiceCollection services, IConfiguration configuration)
		{
			services.Configure<KestrelServerOptions>(
				configuration.GetSection("Kestrel"));
		}

		public static void ConfigureSwagger(this IServiceCollection services, string description = "", string version = "v1")
		{
			var entryAssembly = Assembly.GetEntryAssembly()?.GetName().Name;

			description = description == "" ? $"{entryAssembly}" : description;
			services.AddSwaggerGen(c =>
			{
				c.UseInlineDefinitionsForEnums();
				c.SwaggerDoc(version, new OpenApiInfo
				{
					Version = version,
					Title = description,
					Description = description,
					TermsOfService = new Uri($"{ApiDomain}terms"),
					Contact = new OpenApiContact
					{
						Name = "4Listen",
						Email = "contact4listen@gmail.com",
						Url = new Uri(Domain)
					},
					License = new OpenApiLicense()
					{
						Name = "Licence Info",
						Url = new Uri($"{ApiDomain}licence")
					}
				});

				// Set the comments path for the Swagger JSON and UI.
				var xmlFile = $"{entryAssembly}.xml";
				var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

				if (File.Exists(xmlPath))
				{
					c.IncludeXmlComments(xmlPath);
				}

				c.AddFluentValidationRules();
				var securityScheme = new OpenApiSecurityScheme()
				{

					Type = SecuritySchemeType.Http,
					Scheme = "bearer",
					In = ParameterLocation.Header,
					BearerFormat = "JWT",
					Name = "JWT Authentication"
				};
				c.AddSecurityDefinition("JWTToken", securityScheme);

				var requirement = new OpenApiSecurityRequirement
				{
					{
						new OpenApiSecurityScheme()
						{
							Reference = new OpenApiReference()
							{
								Type = ReferenceType.SecurityScheme, Id = "JWTToken"
							}
						},
						new string[0]
					}
				};


				c.AddSecurityRequirement(requirement);
			});
		}

		public static void BuildSwagger(this IApplicationBuilder app, string name = "")
		{
			name = name == "" ? $"{Assembly.GetEntryAssembly()?.GetName().Name}" : name;

			app.UseSwagger();
		}
	}
}
