using RabbitMQ.Client;
using PublisherService.Models;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PublisherService.Infrastructure
{
    public class RabbitMqPublisher
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private const string QueueName = "ArticleQueue";

        public RabbitMqPublisher(string hostName = "rabbitmq", string user = "guest", string pass = "guest", string vhost = "/")
        {
            var factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = user,
                Password = pass,
                VirtualHost = vhost,
                Port = 5672
            };

            // Create async connection and channel
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            // Declare queue asynchronously
            _channel.QueueDeclareAsync(queue: QueueName, durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
        }
        public async Task PublishArticleAsync(ArticleDto article)
        {
            var json = JsonSerializer.Serialize(article);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                DeliveryMode = (DeliveryModes)2, // persistent
                ContentType = "application/json",
                Headers = new Dictionary<string, object?>
                {
                    ["traceId"] = article.TraceId ?? Guid.NewGuid().ToString()
                }
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
