using ISDOX.DMS.Application.Interfaces;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace ISDOX.DMS.Infrastructure.Messaging
{
    public class RabbitMqPublisher : IMessagePublisher
    {
        private readonly IConnection _connection;

        public RabbitMqPublisher(IConnection connection)
        {
            _connection = connection;
        }

        public async Task PublishAsync<T>(T message, CancellationToken ct = default)
        {
            using var channel = await _connection.CreateChannelAsync(cancellationToken: ct);

            var queueName = typeof(T).Name;

            await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: ct);

            var jsonPayload = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(jsonPayload);

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, body: body, cancellationToken: ct);
        }
    }
}
