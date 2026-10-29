type: fix

The tabs and connectors open a short-lived database context per operation from `IDbContextFactory` instead of holding one for the whole Blazor circuit, every row timestamp comes from the registered `TimeProvider`, and the app panel reads each app's online state once per render.
