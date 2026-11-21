type: fix

The home page now observes its monitor refresh: a scroll that fails because the browser has disconnected is ignored, and any other failure is logged instead of being lost.
