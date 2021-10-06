using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQClientInfrastructure.Configuration;
using RabbitMQClientInfrastructure.EventBus;
using RabbitMQClientInfrastructure.EventMessage;

namespace RabbitMQClientInfrastructure.QueService
{
	public class PersistentQueService : IEventBus, IDisposable
	{
		private readonly IBasicProperties _basicProperties;
		private readonly ILogger<PersistentQueService> _logger;
		private readonly IDefaultConnection _persistentConnection;
		protected readonly QueConfiguration Settings;
		private IModel _consumerChannel;
		private IModel _producerChannel;

		protected PersistentQueService(IDefaultConnection defaultConnection, IOptions<QueConfiguration> settings, ILogger<PersistentQueService> logger)
		{
			Settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_persistentConnection = defaultConnection ?? throw new ArgumentNullException(nameof(defaultConnection));

			_producerChannel = CreateProducerChannel();
			_consumerChannel = Settings.ConsumersCount > 0 ? CreateConsumerChannel() : null;
			_basicProperties = _producerChannel.CreateBasicProperties();
			_basicProperties.Persistent = true;
			_basicProperties.DeliveryMode = 2;
			_basicProperties.ContentType = "application/json";
			_basicProperties.ContentEncoding = Encoding.UTF8.EncodingName;
		}

		public void Dispose()
		{
			if (_consumerChannel?.IsClosed == false)
			{
				QueueUnbind(_consumerChannel);
				_consumerChannel.Close();
				_consumerChannel.Dispose();

			}
			if (_producerChannel?.IsClosed == false)
			{
				QueueUnbind(_producerChannel);
				_producerChannel.Close();
				_producerChannel.Dispose();
			}
			GC.SuppressFinalize(this);
		}


		public void PublishMessage<T>(T eventMessage) where T : QueEventMessage
		{
			if (!_persistentConnection.IsConnected)
			{
				_persistentConnection.TryConnect();
			}
			if (_producerChannel == null)
			{
				_producerChannel = CreateProducerChannel();
			}

			_producerChannel.BasicPublish(Settings.ExchangeName, Settings.RoutingKey, _basicProperties, GetConvertedMessage());

			byte[] GetConvertedMessage()
			{
				if (eventMessage == null)
				{
					throw new ArgumentNullException(nameof(eventMessage));
				}
				return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventMessage, new JsonSerializerSettings
				{
					TypeNameHandling = TypeNameHandling.Auto
				}));
			}
		}

		public void ProcessQueue<T>(Func<T, CancellationToken, Task<bool>> onDequeue, Action<Exception, T> onError, CancellationToken cancellationToken) where T : QueEventMessage
		{
			if (_consumerChannel != null)
			{
				_consumerChannel.Dispose();
				_consumerChannel = CreateConsumerChannel();
			}

			var consumer = new EventingBasicConsumer(_consumerChannel);
			consumer.Received += async (sender, ea) =>
			{
				var str = Encoding.UTF8.GetString(ea.Body.ToArray());
				var parsedMessage = JsonConvert.DeserializeObject<T>(str, new JsonSerializerSettings
				{
					TypeNameHandling = TypeNameHandling.All
				});

				try
				{
					var processingSucceded = await onDequeue.Invoke(parsedMessage, cancellationToken);
					if (processingSucceded)
					{
						MarkAsProcessed(ea.DeliveryTag, false);
					}
					else
					{
						MarkAsNotProcessed(ea.DeliveryTag, false, false);
					}
				}
				catch (OperationCanceledException)
				{
					Dispose();
				}
				catch (Exception e)
				{
					onError.Invoke(e, parsedMessage);
					_logger.LogError(e, "Error On Receiving message");

					MarkAsNotProcessed(ea.DeliveryTag, false, false);
				}
			};

			_consumerChannel.BasicConsume(Settings.QueName, false, consumer);
			consumer.Shutdown += (sender, ea) =>
			{
				if (!cancellationToken.IsCancellationRequested)
				{
					ProcessQueue(onDequeue, onError, cancellationToken);
				}
			};
		}

		public void ProcessBatchQueue<T>(Func<List<T>, CancellationToken, Task<bool>> onDequeue, Action<Exception, List<T>> onError, CancellationToken cancellationToken) where T : QueEventMessage
		{
			if (_consumerChannel != null)
			{
				_consumerChannel.Dispose();
				_consumerChannel = CreateConsumerChannel();
			}

			var messages = new ConcurrentBag<T>();

			var consumer = new EventingBasicConsumer(_consumerChannel);
			consumer.Received += async (sender, ea) =>
			{
				var str = Encoding.UTF8.GetString(ea.Body.ToArray());

				var parsedMessage = JsonConvert.DeserializeObject<T>(str, new JsonSerializerSettings
				{
					TypeNameHandling = TypeNameHandling.All
				});

				messages.Add(parsedMessage);

				if (messages.Count == Settings.PrefetchCount)
				{
					try
					{
						var processingSucceded = await onDequeue.Invoke(messages.ToList(), cancellationToken);
						if (processingSucceded)
						{
							MarkAsProcessed(ea.DeliveryTag, true);
						}
						else
						{
							MarkAsNotProcessed(ea.DeliveryTag, true, false);
						}
					}
					catch (OperationCanceledException)
					{
						Dispose();
					}
					catch (Exception e)
					{
						onError.Invoke(e, messages.ToList());
						_logger.LogError(e, "Error On Receiving message");

						MarkAsNotProcessed(ea.DeliveryTag, false, false);
					}
					finally
					{
						messages.Clear();
					}
				}
				else
				{
					_logger.LogDebug("Prefetching....");
				}
			};

			_consumerChannel.BasicConsume(Settings.QueName, false, consumer);
			consumer.Shutdown += (sender, ea) =>
			{
				if (!cancellationToken.IsCancellationRequested)
				{
					ProcessBatchQueue(onDequeue, onError, cancellationToken);
				}
			};
		}

		private IModel CreateConsumerChannel()
		{
			var channel = CreateBasicChannel();

			if (Settings.WithDeadLetter)
			{
				channel.ExchangeDeclare(Settings.DeadLetterExchangeName, ExchangeType.Fanout);
				channel.QueueDeclare(Settings.DeadLetterQueName, true, false, false);
				channel.QueueBind(Settings.DeadLetterQueName, Settings.DeadLetterExchangeName, string.Empty);

				var arguments = new Dictionary<string, object>()
				{
					{ "x-dead-letter-exchange", Settings.DeadLetterExchangeName }
				};

				channel.QueueDeclare(Settings.QueName, true, false, false, arguments);
			}

			else
			{
				channel.QueueDeclare(Settings.QueName, true, false, false);
			}

			channel.CallbackException += (sender, args) => _consumerChannel = CreateConsumerChannel();

			channel.BasicQos(0, Settings.PrefetchCount, false);

			channel.QueueBind(Settings.QueName, Settings.ExchangeName, Settings.RoutingKey);

			return channel;
		}

		private IModel CreateProducerChannel()
		{
			var channel = CreateBasicChannel();
			channel.ConfirmSelect();
			return channel;
		}

		private IModel CreateBasicChannel()
		{
			if (!_persistentConnection.IsConnected)
			{
				_persistentConnection.TryConnect();
			}

			var channel = _persistentConnection.CreateModel();

			channel.ExchangeDeclare(Settings.ExchangeName, Settings.ExchangeType, true, false);

			return channel;
		}

		private void MarkAsProcessed(ulong deliveryTag, bool multiple)
		{
			if (!_persistentConnection.IsConnected)
			{
				_persistentConnection.TryConnect();
			}
			_logger.LogDebug($"Mark as processed - {deliveryTag}");
			_consumerChannel.BasicAck(deliveryTag, multiple);
		}

		private void MarkAsNotProcessed(ulong deliveryTag, bool multiple, bool requeue)
		{
			if (!_persistentConnection.IsConnected)
			{
				_persistentConnection.TryConnect();
			}
			_logger.LogDebug($"Mark as not processed - {deliveryTag}");
			_consumerChannel.BasicNack(deliveryTag, multiple, requeue);
		}

		private void QueueUnbind(IModel channel)
		{
			if (!string.IsNullOrEmpty(Settings.QueName))
				channel.QueueUnbind(Settings.QueName, Settings.ExchangeName, Settings.RoutingKey);
		}
	}
}
