namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;

    public static class TopicExchangeConfigurationExtensions
    {
        public static void EnableTopicExchange(this EndpointConfiguration config, string topicExchange = null)
        {
            if (!string.IsNullOrWhiteSpace(topicExchange))
            {
                config.GetSettings().Set(TopicExchangeClient.TopicExchangeEndpointNameKey, topicExchange);
            }
            config.EnableFeature<TopicExchangeClient>();
        }

        public static void StartTopicExchangeServer(this EndpointConfiguration config, string topicExchange = null)
        {
            if (!string.IsNullOrWhiteSpace(topicExchange))
            {
                config.GetSettings().Set(TopicExchangeServer.TopicExchangeServerEndpointNameKey, topicExchange);
            }
            config.EnableFeature<TopicExchangeServer>();
        }
    }
}