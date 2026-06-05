# TODO
This file is for tracking ideas for future features, improvements, or refactors. It is not a backlog or issue tracker. Items here may be vague and unprioritized. Do not treat this as a task list or project plan.

## System.Pipelines
Create a new namespace for generic pipelines, middlewares, and related abstractions.

Example features:
- `Pipeline<TContext>`: A generic pipeline that can execute a series of middlewares with a given context.
- `IMiddleware<TContext>`: An interface for middleware components that can be added to the pipeline.
- `PipelineBuilder<TContext>`: A builder for constructing pipelines with a fluent API.
- Support for both synchronous and asynchronous middlewares.
- Branching pipelines or conditional middlewares based on context properties.
- Integration with dependency injection for middleware resolution.
- Built-in middlewares for common tasks like logging, error handling, and performance measurement.
- Extensibility points for custom middleware behaviors, such as short-circuiting or modifying the execution flow.

Example Shared Code:
```csharp
record SampleContext(
	string Data
);

class SampleMiddleware : IMiddleware<SampleContext> {
	public async Task InvokeAsync(SampleContext context, Func<Task> next) {
		// Custom middleware logic
		await next();
	}
}
```

Example usage:
```csharp
var pipeline = new PipelineBuilder<SampleContext>()
	.Use(async (context, next) => {
		// Pre-processing logic
		await next();
		// Post-processing logic
	})
	.Use<SampleMiddleware>()
	.Build();

var result = await pipeline.ExecuteAsync(new(
	Data: "Example data"
));
```

Example usage with DI:
```csharp
var services = new ServiceCollection();
services.AddTransient<SampleMiddleware>();
services.AddPipeline<SampleContext>(pipeline => {
	pipeline.Use<SampleMiddleware>();
});

using var root = services.BuildServiceProvider();
await using var scope = root.CreateAsyncScope();

var pipeline = scope.ServiceProvider.GetRequiredService<Pipeline<SampleContext>>();
var result = await pipeline.ExecuteAsync(new(
	Data: "Example data"
));
```

Advanced usage:
```csharp
var services = new ServiceCollection();

services.AddTransient<SampleMiddleware>();

services.AddPipeline<SampleContext>(pipeline => {
	pipeline.UseLogging(); // Built-in logging middleware
	pipeline.UseErrorHandling(); // Built-in error handling middleware

	pipeline.When(
		condition: context => context.Data.Contains("special"),
		matched: branch => branch
			.Use(async (context, next) => {
				// Special processing for contexts with "special" in the data
				await next();
			}),
		unmatched: branch => branch
			.Use(async (context, next) => {
				// Processing for contexts without "special" in the data
				await next();
			})
	);

	pipeline.Use<SampleMiddleware>();
});

using var root = services.BuildServiceProvider();
await using var scope = root.CreateAsyncScope();

var pipeline = scope.ServiceProvider.GetRequiredService<Pipeline<SampleContext>>();
var result = await pipeline.ExecuteAsync(new(
	Data: "Example data"
));
```