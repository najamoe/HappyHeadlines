using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PublisherService.Infrastructure { 

public class RabbitMqPublisher
{
    private readonly IConnection _connection;
    private readonly IChannel _channel; // async channel
    private const string QueueName = "ArticleQueue";

    public RabbitMqPublisher(string hostName = "localhost", string user = "guest", string pass = "guest", string vhost = "/")
    {
        var factory = new ConnectionFactory
        {
            HostName = "rabbitmq",
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/",
            Port = 5672
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        // Declare queue asynchronously
        _channel.QueueDeclareAsync(queue: QueueName, durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
    }

    public async Task PublishArticleAsync(object article)
    {
        var json = JsonSerializer.Serialize(article);
        var body = Encoding.UTF8.GetBytes(json);

        await _channel.BasicPublishAsync(exchange: "", routingKey: QueueName, body: body);
    }
}

}