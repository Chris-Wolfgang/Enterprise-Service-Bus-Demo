type: fix

A connector handler, auto-response or account-create timeout that fails is now logged instead of silently lost, handlers run after the bus lock is released so one failing handler no longer stops delivery to the others, and pending account-create timers are disposed.
