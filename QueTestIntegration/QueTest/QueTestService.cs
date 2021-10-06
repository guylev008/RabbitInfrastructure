using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQClientInfrastructure.EventBus;
using RabbitMQClientInfrastructure.QueService;

namespace QueTestIntegration.QueTest
{
	public class QueTestService : PersistentQueService, IEventBus
	{
		public QueTestService(IDefaultConnection persistentConnection, IOptions<QueTestConfiguration> settings, ILogger<QueTestService> logger)
			: base(persistentConnection, settings, logger)
		{

		}
	}
}
