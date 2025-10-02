using RabbitMQ.Client;
using System;
using System.Threading.Tasks;
using SubscriberService.Models;

namespace SubscriberService.Infrastructure
{
    public class RabbitMQConnection : IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private const string QueueName = "SubscriberQueue";

        public IChannel Channel => _channel;

        public RabbitMQConnection(string hostName = "rabbitmq", string user = "guest", string pass = "guest", string vhost = "/")
        {
            var factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = user,
                Password = pass,
                VirtualHost = vhost,
                Port = 5672
            };

            // Async creation
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            // Declare queue async
            _channel.QueueDeclareAsync(queue: QueueName, durable: true, exclusive: false, autoDelete: false)
                    .GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel != null)
                await _channel.CloseAsync();

            if (_connection != null)
                await _connection.CloseAsync();
        }

        public async Task PublishSubscriberAsync(Subscriber subscriber)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(subscriber);
            var body = System.Text.Encoding.UTF8.GetBytes(json);
            var props = new BasicProperties
            {
                DeliveryMode = (DeliveryModes)2, // persistent
                ContentType = "application/json"              
            };
            await _channel.BasicPublishAsync(
                exchange: "",
                routingKey: QueueName,
                mandatory: true,
                basicProperties: props,
                body: body
            );
        }
    }
}
