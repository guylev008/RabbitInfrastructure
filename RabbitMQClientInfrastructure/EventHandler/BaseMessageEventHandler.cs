using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQClientInfrastructure.EventBus;
using RabbitMQClientInfrastructure.EventMessage;

namespace RabbitMQClientInfrastructure.EventHandler
{
	public abstract class BaseMessageEventHandler<TQueEventMessage, TEventBus> : IBaseMessageEventHandler
		where TQueEventMessage : QueEventMessage
		where TEventBus : class, IEventBus
	{
		private readonly ILogger<BaseMessageEventHandler<TQueEventMessage, TEventBus>> _logger;
		protected readonly TEventBus EventBus;

		public BaseMessageEventHandler(ILogger<BaseMessageEventHandler<TQueEventMessage, TEventBus>> logger, TEventBus eventBus)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
		}

		protected abstract void RaiseException(Exception ex, TQueEventMessage publishMessage);

		protected abstract Task<bool> OnMessageReceivedAsync(TQueEventMessage message, CancellationToken cancellationToken);

		public void Consume(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{GetType().Name} started");
			try
			{
				EventBus.ProcessQueue<TQueEventMessage>(OnMessageReceivedAsync, RaiseException, cancellationToken);
			}
			catch (OperationCanceledException)
			{
				_logger.LogError($"{GetType().Name} Service cancellation requested");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error on {GetType().Name} Service");
			}
		}
	}
}
