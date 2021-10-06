using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQClientInfrastructure.Configuration
{
	public class QueConfiguration
	{
		public int ConsumersCount { get; set; } = 1;
		public string QueName { get; set; }
		public string ExchangeName { get; set; }
		public string RoutingKey { get; set; } = "";
		public string ExchangeType { get; set; }
		public ushort PrefetchCount { get; set; } = 1;
		public bool WithDeadLetter { get; set; } = true;
		public string DeadLetterExchangeName { get; set; } = "dead-letter-exchange";
		public string DeadLetterQueName { get; set; } = "dead-letter-queue";

		public QueConfiguration() { }
	}
}
