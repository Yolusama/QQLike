using QQLike.Functional.Instructure;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace QQLike.Functional;

public class RabbitMQConsumer(IChannel channel,IProjectLogger logger) : IRabbitMQConsumer
{
    private const ushort DefaultPrefetchCount = 10;
    private readonly SemaphoreSlim _consumeGate = new(1, 1);
    private readonly HashSet<string> _consumingQueues = [];
    private readonly AsyncEventingBasicConsumer _consumer =  new AsyncEventingBasicConsumer(channel);
    private Func<object, BasicDeliverEventArgs, Task>? _messageHandler;
    private AsyncEventHandler<BasicDeliverEventArgs>? _handler;

    public void SetHandler(Func<object, BasicDeliverEventArgs, Task> handler)
    {
        if (_handler is not null)
        {
            _consumer.ReceivedAsync -= _handler;
        }

        _messageHandler = handler;
        _handler = HandleReceivedAsync;
        _consumer.ReceivedAsync += _handler;
    }
    public async Task Consume(string queueName, string exChange, string routeKey)
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

                await channel.ExchangeDeclareAsync(exChange, ExchangeType.Direct, durable: true, autoDelete: false);
                await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
                await channel.QueueBindAsync(queueName, exChange, routeKey);
                await channel.BasicQosAsync(0, DefaultPrefetchCount, false);
                await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: _consumer);
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
        if (_handler is null)
        {
            return;
        }

        _consumer.ReceivedAsync -= _handler;
        _handler = null;
        _messageHandler = null;
    }

    private async Task HandleReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            if (_messageHandler is null)
            {
                await channel.BasicAckAsync(ea.DeliveryTag, false);
                return;
            }

            await _messageHandler(sender, ea);
            await channel.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch (Exception e)
        {
            await logger.LogAsync($"处理队列消息出现异常: {e}", "RabbitMQ消费者");
            try
            {
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
            }
            catch (Exception nackException)
            {
                await logger.LogAsync($"消息拒绝确认失败: {nackException}", "RabbitMQ消费者");
            }
        }
    }
}