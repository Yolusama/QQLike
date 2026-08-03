namespace QQLike.Functional.Instructure;

public interface IRabbitMQProducer
{
    public Task Produce(string queueName,string exChange,string routeKey, string message);
}