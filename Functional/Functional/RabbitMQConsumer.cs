using QQLike.Functional.Instructure;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace QQLike.Functional;

public class RabbitMQConsumer(IConnection connection, IProjectLogger logger) : IRabbitMQConsumer
{
    private const ushort DefaultPrefetchCount = 10;
    private readonly SemaphoreSlim _consumeGate = new(1, 1);
    private readonly HashSet<string> _consumingQueues = [];

    private IChannel? _channel;
    private AsyncEventingBasicConsumer? _consumer;
    private Func<object, BasicDeliverEventArgs, Task>? _messageHandler;
    private AsyncEventHandler<BasicDeliverEventArgs>? _handler;

    public async Task Consume(string queueName, string exChange, string routeKey, Func<object, BasicDeliverEventArgs, Task> handler)
    {
        try
        {
            await _consumeGate.WaitAsync();
            try
            {
                if (_consumingQueues.Contains(queueName))
                {
                    return;
                }

                await EnsureConsumerAsync(handler);
                if (_channel is null || _consumer is null)
                {
                    throw new InvalidOperationException("RabbitMQ consumer channel is not initialized.");
                }

                await _channel.ExchangeDeclareAsync(exChange, ExchangeType.Direct, durable: true, autoDelete: false);
                await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
                await _channel.QueueBindAsync(queueName, exChange, routeKey);
                await _channel.BasicQosAsync(0, DefaultPrefetchCount, false);
                await _channel.BasicConsumeAsync(queueName, autoAck: false, consumer: _consumer);
                _consumingQueues.Add(queueName);
            }
            finally
            {
                _consumeGate.Release();
            }

            await logger.LogAsync($"开始消费队列 {queueName} 的消息，交换机 {exChange}，路由键 {routeKey}", "RabbitMQ消费者");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await logger.LogAsync($"消费消息从队列 {queueName}，交换机 {exChange}，路由键 {routeKey} 出现异常: {e}", "RabbitMQ消费者");
            throw;
        }
    }

    public void RemoveHandler()
    {
        if (_consumer is not null && _handler is not null)
        {
            _consumer.ReceivedAsync -= _handler;
        }

        _handler = null;
        _messageHandler = null;
    }

    private async Task EnsureConsumerAsync(Func<object, BasicDeliverEventArgs, Task> handler)
    {
        if (_channel is null || !_channel.IsOpen)
        {
            _channel = await connection.CreateChannelAsync();
            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumingQueues.Clear();
            _handler = null;
            _messageHandler = null;
        }

        if (_consumer is null)
        {
            throw new InvalidOperationException("RabbitMQ consumer is not initialized.");
        }

        if (_handler is not null)
        {
            _consumer.ReceivedAsync -= _handler;
        }

        _messageHandler = handler;
        _handler = HandleReceivedAsync;
        _consumer.ReceivedAsync += _handler;
    }

    private async Task HandleReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            if (_messageHandler is null)
            {
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
                return;
            }

            await _messageHandler(sender, ea);
            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch (Exception e)
        {
            await logger.LogAsync($"处理队列消息出现异常: {e}", "RabbitMQ消费者");
            try
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
            }
            catch (Exception nackException)
            {
                await logger.LogAsync($"消息拒绝确认失败: {nackException}", "RabbitMQ消费者");
            }
        }
    }
}