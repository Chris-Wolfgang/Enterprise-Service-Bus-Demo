type: internal

`MonitorService` takes its clock from a `TimeProvider`, so tests can drive the auto-response and account-creation delays without waiting.
