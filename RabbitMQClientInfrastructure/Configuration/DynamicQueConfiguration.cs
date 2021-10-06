using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQClientInfrastructure.Configuration
{
	public class DynamicQueConfiguration : QueConfiguration
	{
		public bool CreateIfNotExists { get; set; }

		public override int GetHashCode() => (QueName, ExchangeName, RoutingKey).GetHashCode();

	}
}
