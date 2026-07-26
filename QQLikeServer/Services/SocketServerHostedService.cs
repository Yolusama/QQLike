using QQLike.Services.Interfaces;

namespace QQLike.Services;

//遇到无法避免的生命周期服务冲突时，如单例注入scoped，使用IServiceScopeFactory创建一个新的作用域来解决问题
public class SocketServerHostedService(IServiceScopeFactory scopeFactory) : IHostedService
{
    private IServiceScope? _scope;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _scope = scopeFactory.CreateScope();
        var socketServerService = _scope.ServiceProvider.GetRequiredService<ISocketServerService>();
        socketServerService.Run();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _scope?.Dispose();
        _scope = null;
        return Task.CompletedTask;
    }
}

