using System.Net;
using Polly;
using Polly.Caching;
using Polly.Registry;
using Polly.Timeout;
using Serilog;
using Stashbox;

namespace AutomateThis;

public static class Policies
{
	public const string OpenWeatherMap = "OpenWeatherMap";

	public static readonly HttpStatusCode[] TransientCodes = {
		HttpStatusCode.RequestTimeout,
		HttpStatusCode.InternalServerError,
		HttpStatusCode.BadGateway,
		HttpStatusCode.ServiceUnavailable,
		HttpStatusCode.GatewayTimeout
	};

	private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(Policies));
	private static readonly PolicyRegistry Registry = new();

	public static void AddRegistry(this StashboxContainer services)
	{
		Registry.Add(
			OpenWeatherMap,
			Policy
				.Handle<HttpRequestException>()
				.Or<TimeoutRejectedException>()
				.OrResult<HttpResponseMessage>(r => TransientCodes.Contains(r.StatusCode))
				.WaitAndRetryAsync(new[] {
					TimeSpan.FromSeconds(1),
					TimeSpan.FromSeconds(1),
					TimeSpan.FromSeconds(5)
				})
				.WrapAsync(Policy.TimeoutAsync(TimeSpan.FromSeconds(30)))
		);

		services.RegisterInstance<IReadOnlyPolicyRegistry<string>>(Registry);
		services.RegisterInstance<IConcurrentPolicyRegistry<string>>(Registry);

		services.Register<IAsyncPolicy<Weather.Weather>>(config =>
		{
			config
				.WithSingletonLifetime()
				.WithFactory<IAsyncCacheProvider>(cacheProvider =>
					Policy
						.CacheAsync<Weather.Weather>(
							cacheProvider,
							TimeSpan.FromMinutes(10),
							_ => "CurrentWeather", // Single use policy; force a key name
							onCacheGet: (ctx, key) =>
							{
								Logger
									.ForContext("Context", ctx, true)
									.ForContext("Key", key)
									.Debug("Cache hit for {Key}.");
							},
							onCacheMiss: (ctx, key) =>
							{
								Logger
									.ForContext("Context", ctx, true)
									.ForContext("Key", key)
									.Debug("Cache miss for {Key}.");
							},
							onCachePut: (ctx, key) =>
							{
								Logger
									.ForContext("Context", ctx, true)
									.ForContext("Key", key)
									.Debug("Cache put for {Key}.");
							},
							onCacheGetError: (ctx, key, ex) =>
							{
								Logger
									.ForContext("Context", ctx, true)
									.ForContext("Key", key)
									.Error(ex, "Cache get error for {Key}.");
							},
							onCachePutError: (ctx, key, ex) =>
							{
								Logger
									.ForContext("Context", ctx, true)
									.ForContext("Key", key)
									.Error(ex, "Cache put error for {Key}.");
							}
						)
				);
		});
	}
}
