using System.Text;
using QQLike.Entity.Configuration;
using QQLike.Functional.Instructure;
using RabbitMQ.Client;

namespace QQLike.Functional;

public class RabbitMQProducer(IConnection connection,IChannel channel,IProjectLogger logger) : IRabbitMQProducer
{
    public async Task Produce(string queueName, string exChange,string routeKey, string message)
    {
        try
        {
          await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            var body = Encoding.UTF8.GetBytes(message);
            await channel.ExchangeBindAsync(queueName, exChange, routeKey);
            await channel.BasicPublishAsync(exChange, routeKey, body: body);
            await logger.LogAsync($"消息已发送到队列 {queueName}，交换机 {exChange}，路由键 {routeKey}，消息内容: {message}", "RabbitMQ生产者");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await logger.LogAsync($"发送消息到队列 {queueName} 出现异常: {e}", "RabbitMQ生产者");
            throw;
        }
    }
    
}