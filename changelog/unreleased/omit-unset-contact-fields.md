type: fix

A customer with no e-mail or phone in one system no longer blanks out the e-mail or phone the other systems hold: connectors leave unset contact fields out of `account.updated` / `account.created`, and an empty value arriving over the bus is stored as "not set" instead of an empty string.
