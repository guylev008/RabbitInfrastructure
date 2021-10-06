using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RabbitMQClientInfrastructure.EventHandler
{
    public interface IBaseMessageEventHandler
    {
        void Consume(CancellationToken cancellationToken);
    }
}
