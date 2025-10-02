using RabbitMQ.Client;
using System.Threading.Tasks;

namespace SubscriberService.Infrastructure
{
    public class RabbitMQConnection : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel; // Note: IModel is the correct type

        private const string QueueName = "SubscriberQueue";

        public IModel Channel => _channel;

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

            // Create connection and channel
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare queue
            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
