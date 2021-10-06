using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QueTestIntegration.QueTest;
using RabbitMQClientInfrastructure;
using RabbitMQClientInfrastructure.Configuration;
using RabbitMQClientInfrastructure.EventBus;
using RabbitMQClientInfrastructure.QueService;

namespace ProducePOC
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("select producer implementation poc :" + Environment.NewLine + "1- StaticQueService" + Environment.NewLine + "2- DynamicQueService" + Environment.NewLine + "3- DynamicQueServiceWithDependencyInjection");
			var option = Console.ReadKey();

			var services = ConfigureServices();
			var serviceProvider = services.BuildServiceProvider();

			switch (option.Key)
			{
				case ConsoleKey.D1:
				case ConsoleKey.NumPad1:
					StaticQueService(serviceProvider);
					break;

				case ConsoleKey.D2:
				case ConsoleKey.NumPad2:
					DynamicQueService(serviceProvider);
					break;

				case ConsoleKey.D3:
				case ConsoleKey.NumPad3:
					DynamicQueServiceWithDependencyInjection(serviceProvider);
					break;

				default:
					StaticQueService(serviceProvider);
					break;
			}
		}

		private static void StaticQueService(ServiceProvider serviceProvider)
		{
			var queService = serviceProvider.GetService<QueTestService>();

			int i = 0;
			while (true)
			{
				queService.PublishMessage(new QueTestMessage() { JobId = ++i });
				Thread.Sleep(1000);
			}
		}

		private static void DynamicQueService(ServiceProvider serviceProvider)
		{
			var defaultConnection = serviceProvider.GetService<IDefaultConnection>();
			var loggerFactory = serviceProvider.GetService<ILoggerFactory>();


			int i = 0;

			var queConfiguration = new DynamicQueConfiguration { QueName = "test-que", RoutingKey = "test-key", ExchangeName = "test-exc" };
			var queConfiguration1 = new DynamicQueConfiguration { QueName = "test1-que", RoutingKey = "test1-key", ExchangeName = "test1-exc", CreateIfNotExists = true, ExchangeType = "direct" };

			using (var eventBus = new DynamicQueService(defaultConnection, loggerFactory, queConfiguration))
			using (var eventBus1 = new DynamicQueService(defaultConnection, loggerFactory, queConfiguration1))
			{
				while (true)
				{
					eventBus.PublishMessage(new QueTestMessage() { JobId = ++i });
					eventBus1.PublishMessage(new QueTestMessage() { JobId = ++i });
					Thread.Sleep(1000);
				}
			}
		}

		private static void DynamicQueServiceWithDependencyInjection(ServiceProvider serviceProvider)
		{
			var dynamicQueServiceResolver = serviceProvider.GetService<IDynamicQueServiceResolver>();
			var queConfiguration = new DynamicQueConfiguration { QueName = "test-que", RoutingKey = "test-key", ExchangeName = "test-exc" };
			var eventBus = dynamicQueServiceResolver.ServiceProvider(queConfiguration);

			int i = 0;

			while (true)
			{
				eventBus.PublishMessage(new QueTestMessage() { JobId = ++i });
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
			DynamicQueDependencyRegistry(services);

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

		private static void DynamicQueDependencyRegistry(IServiceCollection services)
		{
			services.AddScoped<IDynamicEventBus>(sp =>
			{
				var loggerFactory = sp.GetService<ILoggerFactory>();
				var defaultConnection = sp.GetService<IDefaultConnection>();
				var queConfiguration = new DynamicQueConfiguration { QueName = "test-que", RoutingKey = "test-key", ExchangeName = "test-exc" };
				return new DynamicQueService(defaultConnection, loggerFactory, queConfiguration);
			});

			services.AddScoped<IDynamicEventBus>(sp =>
			{
				var loggerFactory = sp.GetService<ILoggerFactory>();
				var defaultConnection = sp.GetService<IDefaultConnection>();
				var queConfiguration = new DynamicQueConfiguration { QueName = "test1-que", RoutingKey = "test1-key", ExchangeName = "test1-exc" };
				return new DynamicQueService(defaultConnection, loggerFactory, queConfiguration);
			});

			services.AddDynamicQueServiceResolver();
		}
	}
}
