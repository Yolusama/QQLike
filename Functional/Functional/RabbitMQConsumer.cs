using System.Text;
using QQLike.Functional.Instructure;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace QQLike.Functional;

public class RabbitMQConsumer(IChannel channel,IProjectLogger logger) : IRabbitMQConsumer
{
    private readonly AsyncEventingBasicConsumer _consumer =  new AsyncEventingBasicConsumer(channel);
    private AsyncEventHandler<BasicDeliverEventArgs> _handler;

    public void SetHandler(Func<object, BasicDeliverEventArgs, Task> handler)
    {
        _handler = new AsyncEventHandler<BasicDeliverEventArgs>(handler);
        _consumer.ReceivedAsync += _handler;
    }
    public async Task Consume(string queueName, string exChange, string routeKey)
    {
        try
        {
            await channel.ExchangeDeclareAsync(exChange, ExchangeType.Direct);
            await channel.QueueDeclareAsync(queueName, durable: true);
            await channel.QueueBindAsync(queueName, exChange, routeKey);
            await channel.BasicConsumeAsync(queueName, autoAck: true, consumer: _consumer);
            await logger.LogAsync($"开始消费队列 {queueName} 的消息，交换机 {exChange}，路由键 {routeKey}", "RabbitMQ消费者");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await logger.LogAsync($"消费消息从队列 {queueName} 出现异常: {e}", "RabbitMQ消费者");
            throw;
        }
    }

    public void RemoveHandler()
    {
        _consumer.ReceivedAsync -= _handler;
    }
}