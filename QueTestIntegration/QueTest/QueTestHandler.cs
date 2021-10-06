using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQClientInfrastructure.EventHandler;

namespace QueTestIntegration.QueTest
{
	public class QueTestHandler : BaseMessageEventHandler<QueTestMessage, QueTestService>
	{
		private readonly ILogger<QueTestHandler> _logger;

		public QueTestHandler(ILogger<QueTestHandler> logger, QueTestService queTestService)
			: base(logger, queTestService)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		protected override Task<bool> OnMessageReceivedAsync(QueTestMessage message, CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{message.GuidId} - {message.JobId}");
			Thread.Sleep(1000);
			return Task.FromResult(true);
		}

		protected override void RaiseException(Exception ex, QueTestMessage publishMessage)
		{
			_logger.LogError(ex, $"RaiseException On QueTestMessage : {ex.Message}");
		}
	}
}
