using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RabbitMQClientInfrastructure.Configuration;
using RabbitMQClientInfrastructure.EventBus;
using RabbitMQClientInfrastructure.EventMessage;

namespace RabbitMQClientInfrastructure.QueService
{
	public class DynamicQueService : IDynamicEventBus, IDisposable
	{
		public DynamicQueConfiguration Settings { get; private set; }
		private readonly ILogger<DynamicQueService> _logger;
		private readonly IDefaultConnection _persistentConnection;
		private IModel _producerChannel;

		public DynamicQueService(IDefaultConnection defaultConnection, ILoggerFactory loggerFactory, DynamicQueConfiguration queConfiguration)
		{
			_logger = loggerFactory.CreateLogger<DynamicQueService>();
			_persistentConnection = defaultConnection;
			Settings = queConfiguration;
			Initialize(Settings);
		}

		public void PublishMessage<T>(T eventMessage) where T : QueEventMessage
		{
			if (!_persistentConnection.IsConnected)
			{
				_persistentConnection.TryConnect();
			}

			_producerChannel.BasicPublish(Settings.ExchangeName, Settings.RoutingKey, body: GetConvertedMessage());

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

		/// <summary>
		/// Close channel
		/// </summary>
		public void Dispose()
		{
			if (_producerChannel?.IsClosed == false)
			{
				_producerChannel.Close();
				_producerChannel.Dispose();
			}
			GC.SuppressFinalize(this);
		}

		protected virtual void Initialize(DynamicQueConfiguration settings)
		{
			try
			{
				_producerChannel = CreateProducerChannel();

				if (!IsExchangeExists())
					throw new Exception($"Exchange {Settings.ExchangeName} doesn't exists");

				if (!IsQueueExists())
					throw new Exception($"Queue {Settings.QueName} doesn't exists");

				// Binding queue with exchange if configured.
				if (!string.IsNullOrWhiteSpace(Settings.ExchangeName))
				{
					_producerChannel.QueueBind(Settings.QueName, Settings.ExchangeName, Settings.RoutingKey ?? string.Empty);
				}

			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Failed Initialize QueConfiguration : {JsonConvert.SerializeObject(settings)}");
				throw;
			}
		}

		protected virtual bool IsQueueExists()
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(Settings.QueName))
				{
					try
					{
						_producerChannel.QueueDeclarePassive(Settings.QueName);
					}
					catch (OperationInterruptedException ex)
					{
						if (ex?.ShutdownReason.ReplyCode == 404)
						{
							return false;
						}

						throw;
					}
					catch (Exception)
					{
						throw;
					}
				}

				return true;

			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed check if queue exists");
				throw;
			}
		}

		protected virtual bool IsExchangeExists()
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(Settings.ExchangeName))
				{
					try
					{
						_producerChannel.ExchangeDeclarePassive(Settings.ExchangeName);
					}
					catch (OperationInterruptedException ex)
					{
						if (ex?.ShutdownReason.ReplyCode == 404)
						{
							return false;
						}

						throw;
					}
					catch (Exception)
					{
						throw;
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed check if exchange exists");
				throw;
			}
		}

		private IModel CreateProducerChannel()
		{
			var channel = CreateBasicChannel();

			channel.ConfirmSelect();
			channel.CreateBasicProperties();

			if (Settings.CreateIfNotExists)
			{
				channel.ExchangeDeclare(Settings.ExchangeName, Settings.ExchangeType, true, false);

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

				channel.CallbackException += (sender, args) => _producerChannel = CreateProducerChannel();

				channel.BasicQos(0, Settings.PrefetchCount, false);

			}

			return channel;
		}

		private IModel CreateBasicChannel()
		{
			if (!_persistentConnection.IsConnected)
			{
				_persistentConnection.TryConnect();
			}

			var channel = _persistentConnection.CreateModel();


			return channel;
		}
	}
}
