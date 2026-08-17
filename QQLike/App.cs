using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QQLike.Components;
using QQLike.Entity.Configuration;
using QQLike.Functional;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;
using QQLike.Services;
using QQLike.Services.Interfaces;
using QQLike.ViewModels;
using QQLike.Views;
using QQLike.Views.Group;
using QQLike.Views.Message;
using QQLike.Views.User;
using RabbitMQ.Client;
using SqlSugar;
using Tesseract;
using ConnectionConfig = SqlSugar.ConnectionConfig;

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

    protected override void OnExit(ExitEventArgs e)
    {
        // 先释放容器中的单例资源（如 TesseractEngine），再走 WPF 默认退出流程
        if (ServiceProvider is IDisposable disposable)
            disposable.Dispose();

        base.OnExit(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // 注册服务和依赖项
        services.AddTransient<Index>();

        services.AddTransient<IndexViewModel>();
        services.AddTransient<EntryHeaderViewModel>();
        services.AddSingleton<AppHeaderViewModel>();

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        var config = builder.Build();
        services.AddSingleton<IConfiguration>(config);
        var setting = config.GetSection(nameof(SysSetting)).Get<SysSetting>();
        services.AddSingleton(setting);
        services.AddSingleton<ISqlSugarClient>(_ =>
        {
            /*var sqlSugarClient = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = setting.DbConnectionString,
                DbType = DbType.MySql,
                IsAutoCloseConnection = true
            });*/

            var sqlSugarClient = new SqlSugarScope(new ConnectionConfig
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

            sqlSugarClient.Aop.OnError = ex => { Console.WriteLine($"SQL执行错误: {ex}"); };

            return sqlSugarClient;
        });

        services.AddScoped<IHttpService, HttpService>();
        services.AddScoped<IProjectLogger, ProjectLogger>(_ => new ProjectLogger(setting.LogPath));
        RegisterViewOptions(services);
        services.AddScoped<IWindowFactory, WindowFactory>();
        services.AddRedis(setting.RedisConnectionString);
        AddConfiguration<EmailConfig>(services, config);
        services.AddScoped<IRandomGenerator, RandomGenerator>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddSingleton<ILocalStorage, LocalStorage>();
        services.AddSingleton<ISessionStorage, SessionStorage>();
        services.AddScoped<IApiService, ApiService>();
        services.AddScoped<IUserControlFactory, UserControlFactory>();
        AddRabbitMQ(services, config);
        AddOcrEngine(services);
    }

    private void AddConfiguration<T>(IServiceCollection services, IConfiguration configuration) where T : class
    {
        var type = typeof(T);
        var configInstance = configuration.GetSection(type.Name).Get<T>();
        services.AddSingleton(configInstance);
    }

    private void AddRabbitMQ(IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMQConfig = configuration.GetSection(nameof(RabbitMQConfig)).Get<RabbitMQConfig>();
        services.AddSingleton(rabbitMQConfig);
        var connectionFactory = rabbitMQConfig.MapTo<RabbitMQConfig, ConnectionFactory>();
        var connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();

        services.AddSingleton(connection);
        services.AddScoped<IRabbitMQProducer, RabbitMQProducer>();
        services.AddScoped<IRabbitMQConsumer, RabbitMQConsumer>();
    }

    private void AddOcrEngine(IServiceCollection services)
    {
        services.AddSingleton<TesseractEngine>(_ =>
        {
            var tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            return new TesseractEngine(tessdataPath, "chi_sim+eng", EngineMode.Default);
        });
    }

    private void RegisterViewOptions(IServiceCollection services)
    {
        //每次需要重新建立使用瞬时实例服务
        services.AddTransient<RegisterView>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<MainView>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<LoadingComponent>();
        services.AddTransient<LoadingViewModel>();
        services.AddTransient<MessageComponent>();
        services.AddTransient<MessageViewModel>();
        services.AddTransient<NotificationComponent>();
        services.AddTransient<NotificationViewModel>();
        services.AddTransient<MessageBoxComponent>();
        services.AddTransient<MessageBoxViewModel>();
        services.AddTransient<UserContactView>();
        services.AddTransient<UserContactViewModel>();
        services.AddScoped<UserSearchHeader>();
        services.AddScoped<UserSearchHeaderViewModel>();
        services.AddTransient<UserSearchHeader>();
        services.AddTransient<CommonToolHeaderViewModel>();
        services.AddTransient<CommonToolHeader>();
        services.AddTransient<ComprehensiveSearchViewModel>();
        services.AddTransient<ComprehensiveSearch>();
        services.AddTransient<UserContactManageView>();
        services.AddTransient<UserContactManageViewModel>();
        services.AddTransient<VerificationMessageView>();
        services.AddTransient<VerificationMessageViewModel>();
        services.AddTransient<UserCardViewModel>();
        services.AddTransient<UserCard>();
        services.AddTransient<VerifyDialogViewModel>();
        services.AddTransient<VerifyDialog>();
        services.AddTransient<RemarkDialogViewModel>();
        services.AddTransient<RemarkDialog>();
        services.AddTransient<UserProfileViewModel>();
        services.AddTransient<UserProfileView>();
        services.AddTransient<ChatMessageViewModel>();
        services.AddTransient<ChatMessageView>();
    }
}