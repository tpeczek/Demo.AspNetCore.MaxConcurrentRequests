# Demo.AspNetCore.MaxConcurrentRequests

Sample education project for demonstrating various approaches to implementing concurrent requests limit and requests queue in ASP.NET Core.

The initial `lock` statement based implementation is available in [lock-statement-based-synchronization](https://github.com/tpeczek/Demo.AspNetCore.MaxConcurrentRequests/tree/lock-statement-based-synchronization) branch and has been described here:

- [Implementing concurrent requests limit in ASP.NET Core for fun and education](http://www.tpeczek.com/2017/08/implementing-concurrent-requests-limit.html)

Since that time there's been a number of bug fixes and improvements.

There is also a second, `SemaphoreSlim` based, implementation available in [semaphoreslim-based-synchronization](https://github.com/tpeczek/Demo.AspNetCore.MaxConcurrentRequests/tree/semaphoreslim-based-synchronization) branch.

## Donating

My blog and open source projects are result of my passion for software development, but they require a fair amount of my personal time. If you got value from any of the content I create, then I would appreciate your support by [sponsoring me](https://github.com/sponsors/tpeczek) (either monthly or one-time).

## Copyright and License

Copyright © 2017 - 2025 Tomasz Pęczek

Licensed under the [MIT License](https://github.com/tpeczek/Demo.AspNetCore.MaxConcurrentRequests/blob/master/LICENSE.md)