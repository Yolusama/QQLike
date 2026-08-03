using RabbitMQ.Client.Events;

namespace QQLike.Functional.Instructure;

public interface IRabbitMQConsumer
{
    public void SetHandler(Func<object, BasicDeliverEventArgs, Task> handler);
    public Task Consume(string queueName,string exchangeName,string routeKey);
    public void RemoveHandler();

}