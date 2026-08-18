using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Heum.Infrastructure.Messaging;

public static class MessagingExtensions
{
    /// <summary>
    /// Registers <see cref="IEventPublisher"/> backed by Azure Service Bus. Requires a
    /// <see cref="ServiceBusClient"/> to already be registered (e.g. via
    /// <c>builder.AddAzureServiceBusClient(...)</c>). Use <paramref name="configureTopics"/>
    /// to declare which topic each event type should be published to.
    /// </summary>
    public static TBuilder AddEventPublishing<TBuilder>(
        this TBuilder builder,
        Action<EventTopicRegistry> configureTopics)
        where TBuilder : IHostApplicationBuilder
    {
        var registry = new EventTopicRegistry();
        configureTopics(registry);

        builder.Services.AddSingleton(registry);
        builder.Services.AddSingleton<IEventPublisher, ServiceBusEventPublisher>();

        return builder;
    }
}
