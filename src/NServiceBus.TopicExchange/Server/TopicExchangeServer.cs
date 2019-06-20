using System;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.TopicExchange;
using NServiceBus.Transport;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

class TopicExchangeServer : Feature
{
    public TopicExchangeServer()
    {
        Defaults(s => s.SetDefault(TopicExchangeServerEndpointNameKey, "NServiceBus.TopicExchange"));
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var topicExchangeServerEndpointName = context.Settings.Get<string>(TopicExchangeServerEndpointNameKey);

        var eventDistributor = GetEventDistributor(context);
        var store = GetSubscriptionStore();

        var topicExchange = new TopicExchangeHub(
            topicExchangeServerEndpointName, 
            store,
            eventDistributor);

        context.AddSatelliteReceiver(topicExchange.Name, 
            topicExchange.Name,
            // TODO: Review these settings
            PushRuntimeSettings.Default,
            DefaultRecoverabilityPolicy.Invoke,
            (builder, messageContext) => topicExchange.HandleMessage(messageContext, builder.Build<IDispatchMessages>())
        );
    }

    private static DefaultEventDistributor GetEventDistributor(FeatureConfigurationContext context)
    {
        var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();
        var distributionPolicy = context.Settings.Get<DistributionPolicy>();

        var eventDistributor = new DefaultEventDistributor(
            distributionPolicy,
            i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));
        return eventDistributor;
    }

    // TODO: Make store pluggable
    // TODO: Allow persistent store
    // NOTE: We can't re-use the same storage if we subscribe to our own events or we get an infinite loop
    private static ISubscriptionStorage GetSubscriptionStore() => (ISubscriptionStorage) Activator.CreateInstance(Type.GetType("NServiceBus.InMemorySubscriptionStorage, NServiceBus.Core"));

    public const string TopicExchangeServerEndpointNameKey = nameof(TopicExchangeServerEndpointNameKey);
}