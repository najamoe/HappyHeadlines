using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Monitoring;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using NewsletterService.Infrastructure;

namespace NewsletterService.Consumers
{
    public class ArticleConsumer : BackgroundService
    {
        private readonly RabbitMqConnection _rabbitMqConnection;
        private readonly ILogger<ArticleConsumer> _logger;
        private const string QueueName = "ArticlePublishedQueue";

        public ArticleConsumer(RabbitMqConnection rabbitMqConnection, ILogger<ArticleConsumer> logger)
        {
            _rabbitMqConnection = rabbitMqConnection;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var consumeActivity = Monitoring.MonitorService.ActivitySource.StartActivity("ConsumeArticleEvent", ActivityKind.Consumer);

            await _rabbitMqConnection.DeclareQueueAsync(QueueName);

            var channel = _rabbitMqConnection.Channel;

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    // Deserialize ArticleDTO
                    var article = JsonSerializer.Deserialize<ArticleDTO>(message);

                    _logger.LogInformation("Received Article: {Title} (TraceId: {TraceId})", article?.Title, article.TraceId);

                    // TODO: fetch subscribers & simulate sending newsletter
                    Console.WriteLine($"Newsletter sent for article: {article?.Title}");

                    // Acknowledge message
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing article message");
                    // NACK with requeue true to retry later
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            await channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false, // we control ack manually
                consumer: consumer
            );

            // keep running until service stops
            await Task.Delay(-1, stoppingToken);
        }
    }

    // Example DTO (you already have one in PublisherService, so maybe share via shared lib)
    public class ArticleDTO
    {
        public required string Id { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public required string Author { get; set; }
        public required DateTime PublishedAt { get; set; }
        public required string TraceId { get; set; }
        
    }
}
