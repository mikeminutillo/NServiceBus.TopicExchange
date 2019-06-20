using System;
using NServiceBus;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NServiceBus.TopicExchange;
using NServiceBus.Transport;

class DefaultEventDistributor : IEventDistributor
{
    private IDistributionPolicy distributionPolicy;
    private Func<EndpointInstance, string> transportAddressTranslation;

    public DefaultEventDistributor(IDistributionPolicy distributionPolicy, Func<EndpointInstance, string> transportAddressTranslation)
    {
        this.distributionPolicy = distributionPolicy;
        this.transportAddressTranslation = transportAddressTranslation;
    }

    public string SelectDestination(string endpoint, string[] addresses, MessageContext context)
    {
        // NOTE: We don't deserialize the message so we can't create a logical message
        var distributionContext = new DistributionContext(addresses, NoMessage, context.MessageId, context.Headers, transportAddressTranslation, context.Extensions);
        var strategy = distributionPolicy.GetDistributionStrategy(endpoint, DistributionStrategyScope.Publish);
        var destination = strategy.SelectDestination(distributionContext);
        return destination;
    }

    private static readonly OutgoingLogicalMessage NoMessage = default(OutgoingLogicalMessage);
}
