using System.Linq;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Routing;
using NServiceBus.Routing.MessageDrivenSubscriptions;
using NServiceBus.Transport;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

internal class TopicExchangeClient : Feature
{
    public TopicExchangeClient()
    {
        DependsOn<MessageDrivenSubscriptions>();
        Defaults(s => s.SetDefault(TopicExchangeEndpointNameKey, "NServiceBus.TopicExchange"));
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var exchangeEndpointName = context.Settings.Get<string>(TopicExchangeEndpointNameKey);

        var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();
        var conventions = context.Settings.Get<Conventions>();
        var publishers = context.Settings.Get<Publishers>();
        var endpointInstances = context.Settings.Get<EndpointInstances>();
        var distributionPolicy = context.Settings.Get<DistributionPolicy>();
        
        var topicExchangeAddressManager = new TopicExchangeAddressManager(
            exchangeEndpointName, 
            endpointInstances, 
            i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)), 
            distributionPolicy
        );

        var eventTypes = context.Settings.GetAvailableTypes().Where(conventions.IsEventType).ToArray();

        // Route all Subscription requests to the TopicExchange
        publishers.AddOrReplacePublishers(
            "TopicExchange", 
            eventTypes.Select(
                e => new PublisherTableEntry(e, PublisherAddress.CreateFromEndpointName(exchangeEndpointName))
            ).ToList()
        );

        // Route a copy of all published events to the TopicExchange
        context.RegisterStartupTask(b => new SubscribeTopicExchangeToAllEvents(
            eventTypes, 
            b.Build<ISubscriptionStorage>(),
            topicExchangeAddressManager
        ));

        // Forward all incoming Subscribe/Unsubscribe requests from legacy subscribers to the topic exchange
        context.Pipeline.Replace(
            "ProcessSubscriptionRequests", 
            builder => new RerouteIncomingPubSubMessagesToTopicExchange(topicExchangeAddressManager), 
            "Reroutes incoming Subscribe and Unsubscribe messages to the topic exchange");
    }

    public const string TopicExchangeEndpointNameKey = nameof(TopicExchangeEndpointNameKey);
}