# Introduction

Welcome to the Enterprise Service Bus Demo!

## Overview

An interactive demo of how an ESB can help your organization connect various systems in realtime.

## Key Features

- **Six connected systems** - CRM, ERP, eCommerce, WMS, Customer Portal and Data Warehouse, each with its own tab and in-memory database.
- **Publish / subscribe** - a change in one system is published on a topic and applied by every subscriber.
- **Auto-responses** - the ERP mints customer numbers (`account.create` -> `account.created`) and confirms orders (`order.place` -> `order.placed`); a create with no reply within 10 seconds is flagged.
- **Offline systems** - messages for an offline system queue and are delivered in order when it returns.
- **Message monitor** - every send and receive, with correlation IDs.
- **Configuration as data** - systems and topics come from `apps.json` and `topics.json`.

## Getting Help

- Check the [Getting Started](getting-started.md) guide
- Review the [API Reference](../api/index.md)
- Open an issue on [GitHub Issues](https://github.com/Chris-Wolfgang/Enterprise-Service-Bus-Demo/issues)
