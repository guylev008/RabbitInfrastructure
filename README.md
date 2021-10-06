
for testing and development:

navigate to the folder were docker-compose.yml is.

run in cmd: docker-compose up --build

go to http://localhost:15672

use the following credentials:
username: guest
password: guest

go to http://localhost:15672/#/queues/%2F/test-queue
click on publish message 
paste the following: {JobId:12} to the payload area and click Publish message

RabbitMQClientInfrastructure is a wrapper for RabbitMQ

the following example can be found,

a QueTestIntegration has been setup using our wrapper
a ConsumePOC and ProducePOC Implemented using 

