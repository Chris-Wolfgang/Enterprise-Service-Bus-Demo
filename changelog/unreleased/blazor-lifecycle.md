type: fix

The message log keeps the latest 500 entries, the ERP terminal clock runs on a `PeriodicTimer` driven by the registered `TimeProvider`, a new ERP customer no longer reuses a number the bus already minted, and the monitor and action dialog read each snapshot once per render.
