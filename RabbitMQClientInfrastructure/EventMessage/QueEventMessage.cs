using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQClientInfrastructure.EventMessage
{
    public abstract class QueEventMessage
    {
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public Guid GuidId { get; set; } = Guid.NewGuid();
    }
}
