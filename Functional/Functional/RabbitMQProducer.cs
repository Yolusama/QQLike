using System.Text;
using QQLike.Functional.Instructure;
using RabbitMQ.Client;

namespace QQLike.Functional;

public class RabbitMQProducer(IConnection connection, IProjectLogger logger) : IRabbitMQProducer
{
    public async Task Produce(string queueName, string exChange,string routeKey, string message)
    {
        try
        {
            await using var channel = await connection.CreateChannelAsync();

            // Ensure exchange exists before bind/publish to avoid 404 NOT_FOUND.
            await channel.ExchangeDeclareAsync(
                exchange: exChange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await channel.QueueBindAsync(queueName, exChange, routeKey);

            var body = Encoding.UTF8.GetBytes(message);
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