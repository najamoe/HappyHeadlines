using Microsoft.AspNetCore.Connections;
using RabbitMQ.Client;
using System.Threading.Channels;

namespace NewsletterService.Infrastructure;

public class RabbitMqConnection : IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public IChannel Channel => _channel;

    public RabbitMqConnection()
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";

        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/",
            Port = 5672
        };

        int retries = 0;
        while (true)
        {
            try
            {
                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
                break;
            }
            catch
            {
                retries++;
                if (retries > 10)
                    throw;

                Console.WriteLine("RabbitMQ not ready yet, retrying in 2 seconds...");
                Thread.Sleep(2000);
            }
        }
    }



    public async Task DeclareQueueAsync(string queueName)
    {
        await _channel.QueueDeclareAsync(
            queue: queueName,
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
