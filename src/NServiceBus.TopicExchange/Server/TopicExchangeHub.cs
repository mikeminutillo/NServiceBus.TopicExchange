namespace NServiceBus.TopicExchange
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Routing;
    using Transport;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public class TopicExchangeHub
    {
        public string Name { get; }
        private readonly ISubscriptionStorage store;
        private readonly IEventDistributor distributor;

        public TopicExchangeHub(string name, ISubscriptionStorage store, IEventDistributor distributor)
        {
            Name = name;
            this.store = store;
            this.distributor = distributor;
        }

        public Task HandleMessage(MessageContext context, IDispatchMessages dispatcher)
        {
            var intent = GetMessageIntent(context);

            switch (intent)
            {
                case MessageIntentEnum.Subscribe:
                    return Subscribe(context);
                case MessageIntentEnum.Unsubscribe:
                    return Unsubscribe(context);
                case MessageIntentEnum.Publish:
                    return Publish(context, dispatcher);
                default:
                    throw new Exception($"Unknown message intent {intent} detected on {context.MessageId}");
            }
        }

        public Task Subscribe(MessageContext context) =>
            store.Subscribe(
                new Subscriber(
                    Get(context, Headers.SubscriberTransportAddress, "Subscribe"),
                    Get(context, Headers.SubscriberEndpoint, "Subscribe"))
                , new MessageType(Get(context, Headers.SubscriptionMessageType, "Subscribe")),
                context.Extensions);


        public Task Unsubscribe(MessageContext context) =>
            store.Unsubscribe(
                new Subscriber(
                    Get(context, Headers.SubscriberTransportAddress, "Unsubscribe"),
                    Get(context, Headers.SubscriberEndpoint, "Unsubscribe")),
                new MessageType(Get(context, Headers.SubscriptionMessageType, "Unsubscribe")),
                context.Extensions);

        public async Task Publish(MessageContext context, IDispatchMessages dispatcher)
        {
            var messageTypes = Get(context, Headers.EnclosedMessageTypes, "Publish")
                .Split(EnclosedMessageTypeSeparator)
                .Select(t => new MessageType(t));

            var subscribers = await store.GetSubscriberAddressesForMessage(messageTypes, context.Extensions);

            var logicalSubscribers = subscribers.GroupBy(s => s.Endpoint, s => s.TransportAddress);

            var destinations = logicalSubscribers.Select(subscriber =>
                distributor.SelectDestination(subscriber.Key, subscriber.ToArray(), context));

            var operations = destinations.Select(destination => new TransportOperation(
                new OutgoingMessage(context.MessageId, context.Headers, context.Body),
                new UnicastAddressTag(destination))).ToArray();

            if (!operations.Any())
            {
                // Log that we didn't dispatch anything
                return;
            }

            await dispatcher.Dispatch(new TransportOperations(operations), context.TransportTransaction, context.Extensions);
        }

        private static string Get(MessageContext context, string header, string operation)
        {
            if (!context.Headers.TryGetValue(header, out var result))
            {
                throw new Exception($"Missing header {header} on {context.MessageId}. Not able to {operation}");
            }

            return result;
        }

        private static MessageIntentEnum GetMessageIntent(MessageContext context)
        {
            var intent = default(MessageIntentEnum);
            if (context.Headers.TryGetValue(Headers.MessageIntent, out var intentValue))
            {
                Enum.TryParse(intentValue, true, out intent);
            }
            return intent;
        }


        static readonly char[] EnclosedMessageTypeSeparator =
        {
            ';'
        };
    }
}