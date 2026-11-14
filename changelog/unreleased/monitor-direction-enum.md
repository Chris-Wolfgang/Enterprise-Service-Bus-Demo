type: internal

`MonitorEntry.Direction` is now a `MonitorDirection` enum (`Send`, `Receive`, `Timeout`) instead of a display string; the monitor shows the same `→ SEND` / `← RECV` / `⚠ TIMEOUT` labels through the new `DirectionLabel` property.
