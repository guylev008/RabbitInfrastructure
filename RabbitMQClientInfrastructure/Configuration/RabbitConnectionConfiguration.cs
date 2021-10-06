using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQClientInfrastructure.Configuration
{
    public class RabbitConnectionConfiguration
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string HostName { get; set; }
        public string VirtualHost { get; set; }
        public int? Port { get; set; }
        public double RequestedHeartbeat { get; set; }
        public int? NetworkRecoveryInterval { get; set; }
        public int? RetryConnectionAttempt { get; set; }

        public string ClientProvidedName { get; set; }
    }
}
