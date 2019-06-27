using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Features;
using NServiceBus.Routing;
using NServiceBus.Transport;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

class SubscribeTopicExchangeToAllEvents : FeatureStartupTask
{
    private readonly Type[] eventTypes;
    private readonly ISubscriptionStorage storage;
    private readonly TopicExchangeAddressManager addressManager;

    public SubscribeTopicExchangeToAllEvents(Type[] eventTypes, ISubscriptionStorage storage, TopicExchangeAddressManager addressManager)
    {
        this.eventTypes = eventTypes;
        this.storage = storage;
        this.addressManager = addressManager;
    }

    // TODO: Periodically refresh this as the EndpointInstances get updated
    protected override Task OnStart(IMessageSession session) =>
        Task.WhenAll( from subscriber in addressManager.GetAllTopicExchangeSubscribers()
            from eventType in eventTypes                    
            select storage.Subscribe(
                subscriber,
                new MessageType(eventType), 
                new ContextBag()
            )
        );

    protected override Task OnStop(IMessageSession session) => Task.FromResult(0);
}