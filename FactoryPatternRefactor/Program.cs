using System.Text.Json.Serialization;
using FactoryPatternRefactor.Factories;
using FactoryPatternRefactor.Interfaces;
using FactoryPatternRefactor.Models;
using FactoryPatternRefactor.Senders;
using FactoryPatternRefactor.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("SmtpSettings"));
builder.Services.Configure<SmsSettings>(
    builder.Configuration.GetSection("SmsSettings"));
builder.Services.Configure<SlackSettings>(
    builder.Configuration.GetSection("SlackSettings"));

// Register HttpClient factory for senders
builder.Services.AddHttpClient();

// Register senders (concrete types for ServiceProvider factory resolution)
builder.Services.AddSingleton<EmailNotificationSender>();
builder.Services.AddSingleton<SmsNotificationSender>();
builder.Services.AddSingleton<SlackNotificationSender>();

// Register factory
builder.Services.AddSingleton<INotificationSenderFactory, ServiceProviderNotificationSenderFactory>();

// Register notification service
builder.Services.AddSingleton<NotificationService>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/notify/single", async (
    NotificationRequest request,
    NotificationService notificationService
    ) =>
    {
        if (string.IsNullOrWhiteSpace(request.Recipient))
            return Results.BadRequest("Recipient is required.");
        if (string.IsNullOrWhiteSpace(request.Body))
            return Results.BadRequest("Body is required.");

        try
        {
            var message = new NotificationMessage(request.Recipient, request.Subject, request.Body);
            await notificationService.SendNotificationAsync(request.Channel, message);

            return Results.Ok(new { Status = "Sent", Channel = request.Channel, Recipient = request.Recipient });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: $"Failed to send via {request.Channel}",
                detail: ex.Message,
                statusCode: 502);
        }
    });

app.MapPost("/notify/bulk", async (
    List<NotificationRequest> requests,
    NotificationService notificationService
    ) =>
    {
        if (requests == null || requests.Count == 0)
            return Results.BadRequest("Requests cannot be empty.");

        var tasks = requests.Select(req =>
        (req.Channel,
            new NotificationMessage(req.Recipient, req.Subject, req.Body)))
        .ToList();

        var result = await notificationService.SendNotificationsAsync(tasks);

        return Results.Ok(result);
    });

app.MapGet("/channels", () =>
{
    return Results.Ok(new
    {
        AvailableChannels = Enum.GetNames(typeof(NotificationChannel)),
        Message = "To add a new channel: 1) Add enum value 2) Create sender class 3) Register in DI"
    });
});

app.Run();