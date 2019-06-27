using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Pipeline;
using NServiceBus.Transport;

class RerouteIncomingPubSubMessagesToTopicExchange : Behavior<IIncomingPhysicalMessageContext>
{
    private TopicExchangeAddressManager addressManager;

    public RerouteIncomingPubSubMessagesToTopicExchange(TopicExchangeAddressManager addressManager)
    {
        this.addressManager = addressManager;
    }

    public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
    {
        var intent = context.Message.GetMessageIntent();
        if (intent == MessageIntentEnum.Subscribe || intent == MessageIntentEnum.Unsubscribe)
        {
            var address = addressManager.SelectTopicExchangeAddress(
                context.Message.MessageId,
                context.Message.Headers,
                context.Extensions
            );
            return context.ForwardCurrentMessageTo(address);
        }

        return next();
    }
}