using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQClientInfrastructure.EventMessage;

namespace QueTestIntegration.QueTest
{
	public class QueTestMessage : QueEventMessage
	{
		public int JobId { get; set; }
	}
}
