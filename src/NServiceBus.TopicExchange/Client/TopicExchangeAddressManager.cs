using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

class TopicExchangeAddressManager
{
    private readonly string topicExchangeName;
    private readonly EndpointInstances endpointInstances;
    private readonly Func<EndpointInstance, string> transportAddressTranslation;
    private readonly IDistributionPolicy distributionPolicy;

    public TopicExchangeAddressManager(string topicExchangeName, EndpointInstances endpointInstances, Func<EndpointInstance, string> transportAddressTranslation, IDistributionPolicy distributionPolicy)
    {
        this.topicExchangeName = topicExchangeName;
        this.endpointInstances = endpointInstances;
        this.transportAddressTranslation = transportAddressTranslation;
        this.distributionPolicy = distributionPolicy;
    }

    public string SelectTopicExchangeAddress(string messageId, Dictionary<string, string> headers, ContextBag context)
    {
        var addresses = GetAllTopicExchangeAddresses().ToArray();
        // NOTE: We don't deserialize the message so we can't create a logical message
        var distributionContext = new DistributionContext(addresses, NoMessage, messageId, headers, transportAddressTranslation, context);
        var strategy = distributionPolicy.GetDistributionStrategy(topicExchangeName, DistributionStrategyScope.Send);
        return strategy.SelectDestination(distributionContext);
    }

    private static readonly OutgoingLogicalMessage NoMessage = default(OutgoingLogicalMessage);


    public IEnumerable<string> GetAllTopicExchangeAddresses()
        => endpointInstances.FindInstances(topicExchangeName)
            .Select(transportAddressTranslation);

    public IEnumerable<Subscriber> GetAllTopicExchangeSubscribers()
        => GetAllTopicExchangeAddresses()
            .Select(transportAddress => new Subscriber(transportAddress, topicExchangeName));
}