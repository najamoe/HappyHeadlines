using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ArticleService.Infrastructure;
using ArticleService.Models;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Models;
using System.Diagnostics;

namespace ArticleService.Consumers
{
    public class ArticleConsumer : BackgroundService
    {
        private readonly ILogger<ArticleConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private const string QueueName = "ArticleQueue";

        public ArticleConsumer(ILogger<ArticleConsumer> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = "rabbitmq",
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                Port = 5672
            };

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                using var consumeActivity = MonitorService.ActivitySource.StartActivity("ConsumeArticleQueue", ActivityKind.Consumer);

                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var article = JsonSerializer.Deserialize<ArticleDto>(message);

                    if (article == null)
                    {
                        _logger.LogWarning("Received null article message");
                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                        return;
                    }

                    _logger.LogInformation("Received article from queue: {Title}", article.Title);

                    using var scope = _serviceProvider.CreateScope();
                    var db = GetDbContext("global", scope.ServiceProvider);

                    db.Set<Article>().Add(new Article
                    {
                        Title = article.Title,
                        Content = article.Content,
                        Author = article.Author,
                        Continent = article.Continent,
                        PublishedAt = article.PublishedAt
                    });


                    await db.SaveChangesAsync(stoppingToken);

                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

                    _logger.LogInformation("Saved article {Title} to database.", article.Title);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing article message");
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            await channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer
            );

            _logger.LogInformation("ArticleConsumer is now listening on {QueueName}", QueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }


        private static DbContext GetDbContext(string continent, IServiceProvider services)
        {
            return continent.ToLower() switch
            {
                "africa" => services.GetRequiredService<AfricaDbContext>(),
                "asia" => services.GetRequiredService<AsiaDbContext>(),
                "europe" => services.GetRequiredService<EuropeDbContext>(),
                "northamerica" => services.GetRequiredService<NorthAmericaDbContext>(),
                "southamerica" => services.GetRequiredService<SouthAmericaDbContext>(),
                "oceania" => services.GetRequiredService<OceaniaDbContext>(),
                "antarctica" => services.GetRequiredService<AntarcticaDbContext>(),
                _ => services.GetRequiredService<GlobalDbContext>(),
            };
        }
    }
}
