using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DChat.Services
{
    public sealed class NotificationsService : IDisposable
    {
        private const string exchangeName = "chat";
        private const string routingKey = "";
        private readonly ILogger<NotificationsService> logger;
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly EventingBasicConsumer consumer;

        public event EventHandler<MessageView>? MessageReceived;

        public NotificationsService(IOptions<NotificationsServiceOptions> options, ILogger<NotificationsService> logger)
        {
            if (string.IsNullOrEmpty(options.Value.RabbitMqConnectionString))
                throw new ArgumentException("RabbitMqConnectionString is required", nameof(options));

            var factory = new ConnectionFactory
            {
                Uri = new Uri(options.Value.RabbitMqConnectionString)
            };

            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            channel.ExchangeDeclare(exchangeName, ExchangeType.Fanout);

            var queueName = channel.QueueDeclare().QueueName;
            channel.QueueBind(queueName, exchangeName, routingKey);

            consumer = new EventingBasicConsumer(channel);
            consumer.Received += Received;

            channel.BasicConsume(queueName, autoAck: true, consumer);
            this.logger = logger;
        }

        public void SendMessage(MessageView msg)
        {
            var body = SerializeMessage(msg);
            if (body is null)
                return;

            channel.BasicPublish(exchangeName, routingKey, basicProperties: null, body.Value);
        }

        private void Received(object? sender, BasicDeliverEventArgs e)
        {
            var msg = ParseMessage(e.Body);
            if (msg is null)
                return;

            MessageReceived?.Invoke(this, msg);
        }

        private ReadOnlyMemory<byte>? SerializeMessage(MessageView msg)
        {
            try
            {
                return MessagePackSerializer.Serialize(msg, ContractlessStandardResolver.Options);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error serializing message");
                return null;
            }
        }

        private MessageView? ParseMessage(ReadOnlyMemory<byte> body)
        {
            try
            {
                return MessagePackSerializer.Deserialize<MessageView>(body, ContractlessStandardResolver.Options);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error to parsing message");
                return null;
            }
        }

        public void Dispose()
        {
            consumer.Received -= Received;
            channel.Dispose();
            connection.Dispose();
        }
    }
}
