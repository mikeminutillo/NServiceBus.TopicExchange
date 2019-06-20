namespace NServiceBus.TopicExchange
{
    using Transport;

    public interface IEventDistributor
    {
        string SelectDestination(string endpoint, string[] addresses, MessageContext context);
    }
}