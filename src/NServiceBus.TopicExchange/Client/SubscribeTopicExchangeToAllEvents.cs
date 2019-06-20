using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Features;
using NServiceBus.Routing;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

class SubscribeTopicExchangeToAllEvents : FeatureStartupTask
{
    private readonly Type[] eventTypes;
    private readonly ISubscriptionStorage storage;
    private readonly EndpointInstances endpointInstances;
    private readonly string topicExchangeEndpointName;
    private readonly Func<EndpointInstance, string> transportAddressTranslation;

    public SubscribeTopicExchangeToAllEvents(string topicExchangeEndpointName, Type[] eventTypes, ISubscriptionStorage storage, EndpointInstances endpointInstances, Func<EndpointInstance, string> transportAddressTranslation)
    {
        this.topicExchangeEndpointName = topicExchangeEndpointName;
        this.eventTypes = eventTypes;
        this.storage = storage;
        this.endpointInstances = endpointInstances;
        this.transportAddressTranslation = transportAddressTranslation;
    }

    // TODO: Periodically refresh this as the EndpointInstances get updated
    protected override Task OnStart(IMessageSession session) =>
        Task.WhenAll( from instance in endpointInstances.FindInstances(topicExchangeEndpointName)
            let transportAddress = transportAddressTranslation(instance)
            from eventType in eventTypes                    
            select storage.Subscribe(
                new Subscriber(transportAddress, topicExchangeEndpointName), 
                new MessageType(eventType), 
                new ContextBag()
            )
        );

    protected override Task OnStop(IMessageSession session) => Task.FromResult(0);
}