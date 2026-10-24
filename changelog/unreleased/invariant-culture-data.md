type: fix

Placeholder values (`{yyyy-MM-dd}`, `{hh:mm}`, `{####}`, `{##}`) and the ERP credit-limit input now use the invariant culture, so a non-Gregorian calendar or a decimal-comma locale no longer changes the data written to the bus or the database.
