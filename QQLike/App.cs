using System.Configuration.Internal;
using System.IO;
using System.Net.Http;
using System.Windows;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QQLike.Components;
using QQLike.Entity.Configuration;
using QQLike.Functional;
using QQLike.Functional.Instructure;
using QQLike.Services;
using QQLike.Services.Interfaces;
using QQLike.ViewModels;
using QQLike.Views;
using QQLike.Views.User;
using SqlSugar;

namespace QQLike;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
            
        // 配置依赖注入容器
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
        
        // 启动主窗口
        var index = ServiceProvider.GetRequiredService<Index>();
        index.Show();
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // 注册服务和依赖项
        services.AddSingleton<Index>();
        
        services.AddSingleton<IndexViewModel>();
        services.AddTransient<EntryHeaderViewModel>();
        services.AddSingleton<AppHeaderViewModel>();
        
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        var config =  builder.Build();
        services.AddSingleton<IConfiguration>(config);
        var setting = config.GetSection(nameof(SysSetting)).Get<SysSetting>();
        services.AddSingleton(setting);
        services.AddSingleton<ISqlSugarClient>(_ =>
        {
            var sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = setting.DbConnectionString,
                DbType = DbType.MySql,
                IsAutoCloseConnection = true
            });
            
            sqlSugarClient.Aop.OnLogExecuting = (sql, pars) =>
            {
                Console.WriteLine(sql);
                Console.WriteLine(string.Join(",", pars?.Select(p => $"{p.ParameterName}:{p.Value}")));
            };
            
            sqlSugarClient.Aop.OnError = ex =>
            {
                Console.WriteLine($"SQL执行错误: {ex}");
            };
            
            return sqlSugarClient;
        });
        
        services.AddScoped<IHttpService,HttpService>();
        services.AddScoped<IProjectLogger,ProjectLogger>(_=>new ProjectLogger(setting.LogPath));
        RegisterViewOptions(services);
        services.AddScoped<IWindowFactory,WindowFactory>();
        services.AddRedis(setting.RedisConnectionString);
        AddConfiguration<EmailConfig>(services, config);
        services.AddScoped<IRandomGenerator, RandomGenerator>();
        services.AddScoped<IEmailSender,EmailSender>();
        services.AddSingleton<ILocalStorage,LocalStorage>();
        services.AddSingleton<ISessionStorage,SessionStorage>();
        services.AddScoped<IApiService,ApiService>();
    }

    private void AddConfiguration<T>(IServiceCollection services,IConfiguration configuration) where T : class
    {
        var type = typeof(T);
        var configInstance = configuration.GetSection(type.Name).Get<T>();
        services.AddSingleton(configInstance);
    }

    private void RegisterViewOptions(IServiceCollection services)
    {
        //每次需要重新建立使用瞬时实例服务
        services.AddTransient<RegisterView>();
        services.AddTransient<RegisterViewModel>();
        services.AddSingleton<MainView>();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<LoadingComponent>();
        services.AddTransient<LoadingViewModel>();
        services.AddTransient<MessageComponent>();
        services.AddTransient<MessageViewModel>();
        services.AddTransient<NotificationComponent>();
        services.AddTransient<NotificationViewModel>();
        services.AddTransient<MessageBoxComponent>();
        services.AddTransient<MessageBoxViewModel>();
        services.AddSingleton<UserContactView>();
        services.AddSingleton<UserContactViewModel>();
        services.AddScoped<UserSearchHeader>();
        services.AddScoped<UserSearchHeaderViewModel>();
        services.AddTransient<UserSearchHeader>();
        services.AddTransient<CommonToolHeaderViewModel>();
    }
}