using RabbitMQClientInfrastructure.Configuration;
using RabbitMQClientInfrastructure.EventMessage;

namespace RabbitMQClientInfrastructure.EventBus
{
	public interface IDynamicEventBus
	{
		DynamicQueConfiguration Settings { get; }
		void PublishMessage<T>(T eventMessage) where T : QueEventMessage;
	}
}
