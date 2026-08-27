using FreeSql;
using QQLike.Entity.Configuration;
using QQLike.Functional;
using QQLike.Functional.Instructure;
using QQLike.Services;
using QQLike.Services.Interfaces;
using SysSetting = QQLike.Entity.Configuration.Server.SysSetting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<JwtAuthorizeFilter>();
    options.Filters.Add<GlobalExceptionHandler>();
});

var setting = builder.Configuration.GetSection(nameof(SysSetting)).Get<SysSetting>();

builder.Services.AddSingleton<IFreeSql>(_ =>
{
    var freeSqlBuilder = new FreeSqlBuilder();
    freeSqlBuilder.UseAdoConnectionPool(true)
        .UseConnectionString(DataType.MySql, setting.DbConnectionString)
        .UseAutoSyncStructure(false)
        .UseMonitorCommand(cmd =>
        {
            cmd.CommandTimeout = 180;
            Console.WriteLine(cmd.CommandText);
        });

    return freeSqlBuilder.Build();
});
builder.Services.AddSingleton(setting);
builder.Services.AddSingleton<EmailConfig>(_ => builder.Configuration
    .GetSection(nameof(EmailConfig)).Get<EmailConfig>());
builder.Services.AddSingleton<RabbitMQConfig>(_ => builder.Configuration
    .GetSection(nameof(RabbitMQConfig)).Get<RabbitMQConfig>());
var jwtConfig = builder.Configuration.GetSection(nameof(JwtConfig)).Get<JwtConfig>();
builder.Services.AddSingleton<JwtConfig>(jwtConfig);
var fileConfig = builder.Configuration.GetSection(nameof(FileConfig)).Get<FileConfig>();
builder.Services.AddSingleton(fileConfig);

var rabbitMQConfig = builder.Configuration.GetSection(nameof(RabbitMQConfig)).Get<RabbitMQConfig>();
builder.Services.AddSingleton(rabbitMQConfig);
builder.Services.AddRabbitMQ(rabbitMQConfig);

builder.Services.AddScoped<IProjectLogger, ProjectLogger>(_ => new ProjectLogger(setting.LogPath));
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRandomGenerator, RandomGenerator>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserContactService, UserContactService>();
builder.Services.AddScoped<IVerificationMessageService, VerificationMessageService>();
builder.Services.AddScoped<IHeadMessageService, HeadMessageService>();

builder.Services.AddScoped<JwtAuthorizeFilter>();
builder.Services.AddScoped<GlobalExceptionHandler>();

builder.Services.AddScoped<ISocketServerService, SocketServerService>();
builder.Services.AddHostedService<SocketServerHostedService>();

builder.Services.AddRedis(setting.RedisConnectionString);
/*builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig.Issuer,
            ValidAudience = jwtConfig.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.SecretKey))
        };
        
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // 从请求头中获取Token
                var path = context.Request.Path;
                if (!path.StartsWithSegments("/api"))
                    context.NoResult();
                return Task.CompletedTask;
            }
        };
    });*/
//builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
/*if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}*/

app.HandleStaticFiles(fileConfig);

//app.UseAuthentication();
//app.UseAuthorization();
//app.UseHttpsRedirection();
app.MapControllers();

//app.RunSocketServer();

app.Run();