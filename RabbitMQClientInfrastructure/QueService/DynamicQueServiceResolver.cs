using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RabbitMQClientInfrastructure.Configuration;
using RabbitMQClientInfrastructure.EventBus;

namespace RabbitMQClientInfrastructure.QueService
{
	public interface IDynamicQueServiceResolver
	{
		IDynamicEventBus ServiceProvider(DynamicQueConfiguration queConfiguration);
	}

	public class DynamicQueServiceResolver : IDynamicQueServiceResolver
	{
		private readonly Dictionary<int, IDynamicEventBus> _services;

		public DynamicQueServiceResolver(IEnumerable<IDynamicEventBus> services)
		{
			_services = services.ToDictionary(x => x.Settings.GetHashCode(), x => x);
		}

		public IDynamicEventBus ServiceProvider(DynamicQueConfiguration queConfiguration)
		{
			_services.TryGetValue(queConfiguration.GetHashCode(), out var queService);
			if (queService == null)
				throw new ArgumentException(nameof(queService));

			return queService;
		}
	}
}
