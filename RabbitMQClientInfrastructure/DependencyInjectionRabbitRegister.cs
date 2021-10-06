using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQClientInfrastructure.Configuration;
using RabbitMQClientInfrastructure.QueService;

namespace RabbitMQClientInfrastructure
{
	public static class DependencyInjectionRabbitRegister
	{
		public static void AddRabbitMQSettings(this IServiceCollection services, IConfiguration configuration)
		{
			services.Configure<RabbitConnectionConfiguration>(configuration.GetSection(nameof(RabbitConnectionConfiguration)));

			services.AddSingleton<IDefaultConnection>(sp =>
			{
				var logger = sp.GetRequiredService<ILogger<DefaultConnection>>();
				var config = sp.GetRequiredService<IOptions<RabbitConnectionConfiguration>>().Value;

				var factory = new ConnectionFactory
				{
					HostName = config.HostName,
					VirtualHost = config.VirtualHost,
					Port = config.Port ?? 5672,
					UserName = config.UserName,
					Password = config.Password,
					NetworkRecoveryInterval = TimeSpan.FromSeconds(config.NetworkRecoveryInterval ?? 10),
					RequestedHeartbeat = TimeSpan.FromSeconds(config.RequestedHeartbeat),
					ClientProvidedName = !string.IsNullOrEmpty(config.ClientProvidedName) ? $"{config.ClientProvidedName}-{Guid.NewGuid()}" : "connection name not provided",
					AutomaticRecoveryEnabled = true,
					ContinuationTimeout = TimeSpan.FromSeconds(60)
				};

				return new DefaultConnection(factory, logger, config.RetryConnectionAttempt ?? 5);
			});
		}
	}
}
