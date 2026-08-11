using RabbitMQ.Client.Events;

namespace QQLike.Functional.Instructure;

public interface IRabbitMQConsumer
{
    public Task Consume(string queueName,string exchangeName,string routeKey, Func<object, BasicDeliverEventArgs, Task> handler);
    public void RemoveHandler();

}