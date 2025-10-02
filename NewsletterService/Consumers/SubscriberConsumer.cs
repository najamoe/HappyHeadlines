using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NewsletterService.Infrastructure;

namespace NewsletterService.Consumers;

public class SubscriberConsumer : BackgroundService
{
    private readonly RabbitMqConnection _rabbitMqConnection;
    private const string QueueName = "SubscriberQueue";

    public SubscriberConsumer(RabbitMqConnection rabbitMqConnection)
    {
        _rabbitMqConnection = rabbitMqConnection;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _rabbitMqConnection.DeclareQueueAsync(QueueName);

        var channel = _rabbitMqConnection.Channel;
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = System.Text.Encoding.UTF8.GetString(body);
                Console.WriteLine($"Received Subscriber: {message}");
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing subscriber message: {ex.Message}");
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer
        );

        await Task.Delay(Timeout.Infinite, stoppingToken); // keep running
    }
}

