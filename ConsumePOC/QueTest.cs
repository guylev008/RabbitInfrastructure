using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QueTestIntegration.QueTest;
using RabbitMQClientInfrastructure.EventHandler;

namespace ConsumePOC
{
	public class QueTest : BackgroundService
    {
        protected readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Task, IServiceScope> _taskScopes;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly IOptions<QueTestConfiguration> _queTestConfiguration;

        public QueTest(IServiceProvider serviceProvider, IOptions<QueTestConfiguration> queTestConfiguration)
        {
            _serviceProvider = serviceProvider;
            _queTestConfiguration = queTestConfiguration;
            _taskScopes = new Dictionary<Task, IServiceScope>();
        }

        protected virtual void Start<TEventHandler>(CancellationToken cancellationToken, int consumerCount) where TEventHandler : IBaseMessageEventHandler
        {
            for (var i = 0; i < consumerCount; i++)
            {
                var scope = _serviceProvider.CreateScope();

                var task = Task.Factory.StartNew(() =>
                {
                    var handler = scope.ServiceProvider.GetRequiredService<TEventHandler>();
                    handler.Consume(cancellationToken);
                }, TaskCreationOptions.LongRunning);

                _taskScopes.Add(task, scope);
            }
        }

        public override void Dispose()
        {
            foreach (var taskScope in _taskScopes)
            {
                taskScope.Value?.Dispose();
                taskScope.Key?.Dispose();
            }
            base.Dispose();
        }

		protected override Task ExecuteAsync(CancellationToken cancellationToken)
		{
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Start<QueTestHandler>(_cancellationTokenSource.Token, _queTestConfiguration.Value.ConsumersCount);
            return Task.CompletedTask;
        }
	}
}
