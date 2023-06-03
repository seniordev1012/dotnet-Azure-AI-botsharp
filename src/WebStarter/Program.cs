using BotSharp.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();

// Add BotSharp
builder.Services.AddBotSharp(builder.Configuration);
builder.Services.AddBotSharpPlatform(builder.Configuration);
// builder.Services.AddAzureOpenAiPlatform(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("MyCorsPolicy",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

// Use BotSharp
app.UseBotSharp();

#if DEBUG
app.UseCors("MyCorsPolicy");
#endif

app.Run();
