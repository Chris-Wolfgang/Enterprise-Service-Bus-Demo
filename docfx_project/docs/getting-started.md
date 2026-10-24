# Getting Started

This guide gets the Enterprise Service Bus Demo running locally.

## Prerequisites

- The [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Run the demo

```bash
git clone https://github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo.git
cd Enterprise-Service-Bus-Demo
dotnet run --project src/EsbDemo.Web
```

Open the URL printed in the console (by default `http://localhost:5281`).

## Try it

1. Open the **CRM** tab and create a customer. The CRM publishes `account.create`; the ERP answers with `account.created`, and the other systems add the customer.
2. Take a system **offline** in the app panel, make another change, and watch the message queue for it. Bring it back online and the queued messages are delivered in order.
3. Watch the **message monitor** on the home page: every send and receive is logged with its correlation ID.

## Configure the apps and topics

The systems and messages are defined in `src/EsbDemo.Web/config/`:

- `apps.json` - each system's name, display order, default online state, the topics it publishes and subscribes to, and its auto-responses.
- `topics.json` - each topic's label, monitor colour and fields. Field defaults may use `{yyyy-MM-dd}`, `{hh:mm}` (24-hour time), `{####}` and `{##}` (random numbers).

## Next Steps

- Explore the [API Reference](../api/index.md)
- Read the [Introduction](introduction.md)

## Additional Resources

- [GitHub Repository](https://github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo)
- [Contributing Guidelines](https://github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo/blob/main/CONTRIBUTING.md)
- [Report an Issue](https://github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo/issues)
