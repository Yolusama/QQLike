using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using QQLike.Entity.Configuration;
using QQLike.Entity.VO;
using QQLike.Functional;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;
using QQLike.Services.Interfaces;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace QQLike.Services;

public static class ExpansionService
{
    public static void AddRedis(this IServiceCollection services,string redisConnectionString)
    {
        var redisConnect = ConnectionMultiplexer.Connect(redisConnectionString);
        services.AddSingleton<IConnectionMultiplexer, ConnectionMultiplexer>(_ => redisConnect);
        services.AddScoped<IRedisCache, RedisCache>();
    }

    public static void HandleStaticFiles(this WebApplication app,FileConfig config)
    {
        var root = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), config.FileRootPath));
        if (!root.Exists)
            root.Create();
        var images = new DirectoryInfo(Path.Combine(root.FullName, config.ImagePath));
        if (!images.Exists)
            images.Create();
        var videos = new DirectoryInfo(Path.Combine(root.FullName, config.VideoPath));
        if (!videos.Exists)
            videos.Create();

        app.UseStaticFiles(new StaticFileOptions
        {
            ServeUnknownFileTypes = true,
            DefaultContentType = "application/octet-stream",
            FileProvider = new PhysicalFileProvider(root.FullName),
            RequestPath = "/files"
        });
    }

    public static void AddRabbitMQ(this IServiceCollection services, RabbitMQConfig config)
    {
        var connectionFactory = config.MapTo(new ConnectionFactory());
        var connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();

        services.AddSingleton(connection);
        services.AddScoped<IRabbitMQProducer, RabbitMQProducer>();
        services.AddScoped<IRabbitMQConsumer,RabbitMQConsumer>();
    }

    public static UserTokenInfo GetJwtData(this ControllerBase controller)
    {
        var headers = controller.Request.Headers;
        var token = headers["Authorization"].ToString().Split(' ').Last();
        var jwtService = controller.HttpContext.RequestServices.GetRequiredService<IJwtService>();
        var userTokenInfo = jwtService.Parse<UserTokenInfo>(token);
        return userTokenInfo;
    }

    /*private static IServiceScope? _socketServerScope;
    public static void RunSocketServer(this WebApplication app)
    {
        if (_socketServerScope != null)
            return;

        _socketServerScope = app.Services.CreateScope();
        var socketServer = _socketServerScope.ServiceProvider.GetRequiredService<ISocketServerService>();
        socketServer.Run();

        app.Lifetime.ApplicationStopping.Register(() =>
        {
            _socketServerScope?.Dispose();
            _socketServerScope = null;
        });
    }*/
}