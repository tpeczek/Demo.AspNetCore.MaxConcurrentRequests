# Demo.AspNetCore.MaxConcurrentRequests

Sample education project for demonstrating various approaches to implementing concurrent requests limit and requests queue in ASP.NET Core.

The initial `lock` statement based implementation has been described here:

- [Implementing concurrent requests limit in ASP.NET Core for fun and education](http://www.tpeczek.com/2017/08/implementing-concurrent-requests-limit.html)

Since that time there's been a number of bug fixes and improvments.

There is also a second, `SemaphoreSlim` based, implementation available in [branch](https://github.com/tpeczek/Demo.AspNetCore.MaxConcurrentRequests/tree/semaphoreslim-based-synchronization).

## Donating

My blog and open source projects are result of my passion for software development, but they require a fair amount of my personal time. If you got value from any of the content I create, then I would appreciate your support by [buying me a coffee](https://www.buymeacoffee.com/tpeczek).

<a href="https://www.buymeacoffee.com/tpeczek"><img src="https://www.buymeacoffee.com/assets/img/custom_images/black_img.png" style="height: 41px !important;width: 174px !important;box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;-webkit-box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;"  target="_blank"></a>
