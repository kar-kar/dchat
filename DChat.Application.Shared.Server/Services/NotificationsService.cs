using DChat.Application.Shared.ClientServer;
using MessagePack;
using MessagePack.Resolvers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DChat.Application.Shared.Server.Services
{
    public sealed class NotificationsService : IAsyncDisposable
    {
        private readonly ILogger<NotificationsService> logger;
        private readonly Task<RabbitMq> rabbitTask;

        public event EventHandler<MessageView>? MessageReceived;

        public NotificationsService(IConnection connection, ILogger<NotificationsService> logger)
        {
            rabbitTask = RabbitMq.Connect(connection, Received);
            this.logger = logger;
        }

        public async Task SendMessage(MessageView msg)
        {
            var body = SerializeMessage(msg);
            if (body is null)
                return;

            var rabbit = await rabbitTask;
            await rabbit.Send(body.Value);
        }

        private Task Received(ReadOnlyMemory<byte> body)
        {
            var msg = ParseMessage(body);

            if (msg is not null)
                MessageReceived?.Invoke(this, msg);

            return Task.CompletedTask;
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

        public async ValueTask DisposeAsync()
        {
            var rabbit = await rabbitTask;
            await rabbit.DisposeAsync();
        }

        private class RabbitMq: IAsyncDisposable
        {
            private const string exchangeName = "chat";
            private const string routingKey = "";

            public required IChannel Channel { get; init; }
            public required AsyncEventingBasicConsumer Consumer { get; init; }
            public required Func<ReadOnlyMemory<byte>, Task> Receive { get; init; }

            public ValueTask Send(ReadOnlyMemory<byte> body)
            {
                var props = new BasicProperties
                {
                    ContentType = "application/octet-stream",
                    DeliveryMode = DeliveryModes.Transient
                };

                return Channel.BasicPublishAsync(exchangeName, routingKey, mandatory: true, basicProperties: props, body);
            }

            public static async Task<RabbitMq> Connect(IConnection connection, Func<ReadOnlyMemory<byte>, Task> receive)
            {
                var channel = await connection.CreateChannelAsync();
                await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Fanout);
                var queueName = (await channel.QueueDeclareAsync()).QueueName;
                await channel.QueueBindAsync(queueName, exchangeName, routingKey);
                var consumer = new AsyncEventingBasicConsumer(channel);

                var rabbitMq = new RabbitMq
                {
                    Channel = channel,
                    Consumer = consumer,
                    Receive = receive
                };

                consumer.ReceivedAsync += rabbitMq.ReceiveAsync;
                await channel.BasicConsumeAsync(queueName, autoAck: true, consumer);

                return rabbitMq;
            }

            private Task ReceiveAsync(object? sender, BasicDeliverEventArgs e)
            {
                return Receive(e.Body);
            }

            public async ValueTask DisposeAsync()
            {
                Consumer.ReceivedAsync -= ReceiveAsync;
                await Channel.DisposeAsync();
            }
        }
    }
}
