using System;
using System.IO;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace RabbitMQClientInfrastructure.QueService
{
	public class DefaultConnection : IDefaultConnection
	{
		private readonly IConnectionFactory _connectionFactory;
		private readonly ILogger<DefaultConnection> _logger;
		private readonly int _retryCount;
		private readonly object _syncRoot = new object();
		private IConnection _connection;
		private bool _disposed;

		public DefaultConnection(IConnectionFactory connectionFactory, ILogger<DefaultConnection> logger, int retryCount = 5)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
			_retryCount = retryCount;
		}

		public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

		public IModel CreateModel()
		{
			if (!IsConnected)
				throw new InvalidOperationException("Unable to create model with missing connection to RabbitMQ message broker");

			return _connection.CreateModel();
		}

		public void Dispose()
		{
			if (_disposed) return;

			_disposed = true;

			try
			{
				_connection.Dispose();
			}
			catch (IOException ex)
			{
				_logger.LogCritical(ex.ToString());
			}
		}

		public bool TryConnect()
		{
			_logger.LogInformation("Try to connect to Rabbit MQ");

			if (!IsConnected)
			{
				lock (_syncRoot)
				{
					if (!IsConnected)
					{
						var policy = Policy.Handle<SocketException>()
						.Or<BrokerUnreachableException>()
						.WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
							(ex, time) =>
							{
								_logger.LogWarning(ex.ToString());
							}
						);

						policy.Execute(() =>
						{
							if (!IsConnected)
							{
								_logger.LogInformation($"[*] connection : {_connection != null}, connection.IsOpen : {_connection?.IsOpen}, disposed : {_disposed}");
								_connection = _connectionFactory.CreateConnection();
							}
						});

						if (IsConnected)
						{
							_connection.ConnectionShutdown += OnConnectionShutdown;
							_connection.CallbackException += OnCallbackException;
							_connection.ConnectionBlocked += OnConnectionBlocked;

							_logger.LogInformation($"Successfully connected to Rabbit MQ {_connection.Endpoint.HostName} and connected to handling events");

							return true;
						}
						_logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");

						return false;
					}
					else
					{
						_logger.LogInformation($"Reused connection double safe");
					}
				}
			}

			return true;
		}

		private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
		{
			if (_disposed)
				return;

			_logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

			TryConnect();
		}

		private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
		{
			if (_disposed)
				return;

			_logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

			TryConnect();
		}

		private void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
		{
			if (_disposed)
				return;

			_logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

			TryConnect();
		}
	}
}
