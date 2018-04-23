# WebApi.Monitoring

Middleware for monitoring ASP.NET Core WebApis.

# Correlation Token

Adds ```"correlationToken"``` guid  to the headers of incoming requests in order to facilitate requests tracking across various WebApis. If a ```"correlationToken"``` guid is already present in the headers of incoming requests it is reused for the current request.

To add this functionality add ```app.UseCorrelationToken();``` to your ```Configure``` method.

# Performance Logging

Adds performance logging for all the requests to the WebApi. A sample log output is 

```
Request: GET /api/values served in 8ms from GIRPDTP026
```
Please note that structured logging is used so Serilog and other logging facilities can take advantage of it. Please [see this link](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1&tabs=aspnetcore2x#log-message-template) for more information.

```
Request: {Method} {Path} served in {ElapsedMilliseconds}ms from {MachineName}
```

To add this functionality add ```app.UsePerformanceLogging();``` to your ```Configure``` method.

# Request Logging

Adds request logging for all the calls to the WebApi. Sample output:

```
Incoming request: GET, /api/values, [Cache-Control, max-age=0], [Connection, keep-alive], [Accept, text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8], [Accept-Encoding, gzip, deflate, br], [Accept-Language, en-US,en;q=0.9,ca;q=0.8,ro;q=0.7], [Host, localhost:5000], [User-Agent, Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.186 Safari/537.36], [Upgrade-Insecure-Requests, 1], [DNT, 1]

[...]

Outgoing response: 200, [Date, Mon, 23 Apr 2018 09:53:47 GMT], [Transfer-Encoding, chunked], [Content-Type, application/json; charset=utf-8], [Server, Kestrel], [Correlation-Token, 5304427e-1423-4958-bfc7-034989b16870]

```
To add this functionality add ```app.UseRequestLogging();``` to your ```Configure``` method.

# Health Check

Health check adds two endpoints to your WebApi the ping and the healthcheck endpoints. These two endpoints are prefixed by a ```_monitoring``` prefix and this can be configured when you register the middleware. 

When you call the ```/_monitoring/ping``` endpoint a ```204 (No Content)``` will be returned to the caller signifying that the WebApi is up. 

When you call the ```/_monitoring/healthcheck``` endpoint a list of health checks is returned to the caller along with the ```200 (OK)``` status code if all is fine or ```503 (Service Unavailable)``` if critical dependencies are down. 

When you register this middleware you have to provide a function that executes the health checks against the dependecies of your WebApi. This function returns an array of HealthCheck instances:

```csharp
    public struct HealthCheck
    {
        public string DependencyName { get; }
        public bool IsDown { get; }
        public int ResponseTimeMilliseconds { get; }
        public bool IsCritical { get; }
        public string MachineName { get; set; }
    }
```
A possible registration of this middleware is:

```csharp
    app.UseHealthChecks(() => Task.FromResult(new[] {
        new HealthCheck(dependencyName: "sample dependency", isDown: false, responseTimeMilliseconds: 100)
    }));
```

Of course you should replace this code with your own methods that check if your SQL instance or Redis or whatever other apis that you depend on are reachable. Note that for other WebApis you can use the ping endpoint to establish if they are reachable or not. 

Enjoy!
