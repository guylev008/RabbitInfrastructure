using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQClientInfrastructure.EventBus;
using RabbitMQClientInfrastructure.EventMessage;

namespace RabbitMQClientInfrastructure.EventHandler
{
	public abstract class BaseBatchMessageEventHandler<TQueEventMessage, TEventBus> : IBaseMessageEventHandler
		where TQueEventMessage : QueEventMessage
		where TEventBus : class, IEventBus
	{
		private readonly ILogger<BaseBatchMessageEventHandler<TQueEventMessage, TEventBus>> _logger;
		protected readonly TEventBus EventBus;

		public BaseBatchMessageEventHandler(ILogger<BaseBatchMessageEventHandler<TQueEventMessage, TEventBus>> logger, TEventBus eventBus)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
		}

		protected abstract void RaiseException(Exception ex, List<TQueEventMessage> publishMessages);

		protected abstract Task<bool> OnMessageReceivedAsync(List<TQueEventMessage> messages, CancellationToken cancellationToken);

		public void Consume(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{GetType().Name} started");
			try
			{
				EventBus.ProcessBatchQueue<TQueEventMessage>(OnMessageReceivedAsync, RaiseException, cancellationToken);
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
