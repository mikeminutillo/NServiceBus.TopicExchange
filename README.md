# NServiceBus.TopicExchange

NOT SAFE FOR PRODUCTION USE!!!

Taking publisher configuration out of Message-Driven Pub-Sub.

Nominate one (or more) endpoint to run a topic exchange server.

All endpoints connect to a topic exchange server and route all events and subscription requests through it.

When the topic exchange server receives an event, it sends a copy to every endpoint that has subscribed to it.

## Configuring clients

On each endpoint add the following configuration:

```csharp
endpointConfiguration.EnableTopicExchange("TopicExchangeEndpointName");

// OR

endpointConfiguration.EnableTopicExchange(); // Defaults the name to NServiceBus.TopicExchange
```

## Configuring the server

On the endpoint you want to host the topic exchange, add the following configuration:

```csharp
endpointConfiguration.StartTopicExchangeServer("TopicExchangeEndpointName");

// OR

endpointConfiguration.StartTopicExchangeServer(); // Defaults the name to NServiceBus.TopicExchange
```

## Notes

- The topic exchange endpoint name is a logical endpoint name, not a physical address.
- The topic exchange server only stores subscriptions in memory at the moment.
- Does respect sender-side distribution. The server can have mutliple physical addresses for the same logical endpoint and distribute between them.
  - This includes respecting the configured distribution policy on the topic exchange server.

## TODO

- Testing
- Persistent Subscriptions
- Stand-alone server (on top of NSB.Raw)

## Crazy future ideas

- Publishers could periodically request the routing table and just do normal message-driven pub-sub with that
  - That would allow dropping the subscription persistence from the endpoint (just send to the topic exchange until you get a routing table back)