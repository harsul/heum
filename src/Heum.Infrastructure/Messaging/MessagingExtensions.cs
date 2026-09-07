using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Heum.Infrastructure.Messaging;

public static class MessagingExtensions
{
    /// <summary>
    /// Registers <see cref="IEventPublisher"/> with a transport selected by configuration.
    /// When <c>EventBus:Transport</c> is <c>"InProcess"</c>, registers
    /// <see cref="InProcessEventPublisher"/> (no Service Bus required). Otherwise, registers
    /// <see cref="ServiceBusEventPublisher"/> which requires a <see cref="ServiceBusClient"/>
    /// already registered (e.g. via <c>builder.AddAzureServiceBusClient(...)</c>).
    /// Use <paramref name="configureTopics"/> to declare which topic each event type maps to.
    /// </summary>
    public static TBuilder AddEventPublishing<TBuilder>(
        this TBuilder builder,
        Action<EventTopicRegistry> configureTopics)
        where TBuilder : IHostApplicationBuilder
    {
        var registry = new EventTopicRegistry();
        configureTopics(registry);

        builder.Services.AddSingleton(registry);

        if (string.Equals(builder.Configuration["EventBus:Transport"], "InProcess", StringComparison.OrdinalIgnoreCase))
            builder.Services.AddSingleton<IEventPublisher, InProcessEventPublisher>();
        else
            builder.Services.AddSingleton<IEventPublisher, ServiceBusEventPublisher>();

        return builder;
    }
}
