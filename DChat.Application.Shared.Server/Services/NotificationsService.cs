using DChat.Application.Shared.ClientServer;
using MessagePack;
using MessagePack.Resolvers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DChat.Application.Shared.Server.Services
{
    public sealed class NotificationsService : IDisposable
    {
        private const string exchangeName = "chat";
        private const string routingKey = "";
        private readonly ILogger<NotificationsService> logger;
        private readonly IModel channel;
        private readonly EventingBasicConsumer consumer;

        public event EventHandler<MessageView>? MessageReceived;

        public NotificationsService(IConnection connection, ILogger<NotificationsService> logger)
        {
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
        }
    }
}
