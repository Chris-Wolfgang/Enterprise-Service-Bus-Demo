---
_layout: landing
---

# Enterprise Service Bus Demo Documentation

An interactive demo of how an ESB can help your organization connect various systems in realtime.

## Quick Links

- [Getting Started](docs/getting-started.md) - Run the demo and change its configuration
- [API Reference](api/index.md) - Generated from the source code's XML comments
- [GitHub Repository](https://github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo) - View source code

## About the demo

A Blazor Server application in which six simulated systems (CRM, ERP, eCommerce, WMS, Customer Portal and Data Warehouse) exchange messages over an in-memory bus. A change made in one system is published on a topic, every subscribing system updates its own data, systems can be taken offline to see messages queue, and a monitor logs every message.

This is an application, not a library: nothing is published as a NuGet package.

## Documentation Sections

### 📖 [Documentation](docs/getting-started.md)
How to run the demo and how the apps and topics are configured.

### 📚 [API Reference](api/index.md)
API documentation for `EsbDemo.Core` and `EsbDemo.Web`, generated from source code XML comments.

## Additional Resources

- [Contributing Guidelines](https://github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo/blob/main/CONTRIBUTING.md)
- [Code of Conduct](https://github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo/blob/main/CODE_OF_CONDUCT.md)
- [License](https://github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo/blob/main/LICENSE)

---

*Documentation built with [DocFX](https://dotnet.github.io/docfx/)*
