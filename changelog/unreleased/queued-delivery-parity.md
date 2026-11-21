type: fix

A message delivered from an offline system's queue now reads and behaves like a live delivery: the sender's own copy is marked "Self-originated — no action taken" and never fires an auto-response, and a queued `account.updated` shows the same old-to-new diff.
