using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQClientInfrastructure.EventMessage;

namespace RabbitMQClientInfrastructure.EventBus
{
	public interface IEventBus
	{
		//Single Node
		void PublishMessage<T>(T message) where T : QueEventMessage;
		void ProcessQueue<T>(Func<T, CancellationToken, Task<bool>> onDequeue, Action<Exception, T> onError, CancellationToken cancellationToken) where T : QueEventMessage;

		//Multi Nodes
		void ProcessBatchQueue<T>(Func<List<T>, CancellationToken, Task<bool>> onDequeue, Action<Exception, List<T>> onError, CancellationToken cancellationToken) where T : QueEventMessage;
	}
}
