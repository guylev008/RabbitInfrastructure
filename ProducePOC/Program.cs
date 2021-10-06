using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QueTestIntegration.QueTest;
using RabbitMQClientInfrastructure;

namespace ProducePOC
{
	class Program
	{
		static void Main(string[] args)
		{
			var services = ConfigureServices();

			var serviceProvider = services.BuildServiceProvider();

			var queService = serviceProvider.GetService<QueTestService>();

			int i = 0;
			while (true)
			{
				queService.PublishMessage(new QueTestMessage() { JobId = ++i });
				Thread.Sleep(1000);
			}

		}

		private static IServiceCollection ConfigureServices()
		{
			var services = new ServiceCollection();
			var config = LoadConfiguration();

			services.AddLogging(builder =>
			{
				builder.AddConfiguration(config.GetSection("Logging"));
				builder.AddConsole();
			});

			services.AddSingleton(config);
			services.AddRabbitMQSettings(config);
			QueTestTestDependencyRegistry(services, config);

			return services;
		}

		public static IConfiguration LoadConfiguration()
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

			return builder.Build();
		}

		private static void QueTestTestDependencyRegistry(IServiceCollection services, IConfiguration configuration)
		{
			services.Configure<QueTestConfiguration>(configuration.GetSection(nameof(QueTestConfiguration)));
			services.AddScoped<QueTestService, QueTestService>();
		}
	}
}
