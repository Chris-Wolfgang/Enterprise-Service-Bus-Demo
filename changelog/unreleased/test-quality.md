type: internal

The auto-response delay and account-create timeout are named constants the tests share, and the tests that change the ambient `Activity` no longer run in parallel with others.
