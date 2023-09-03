using AutoMapper;
using AutomateThis.Providers.OpenWeatherMap;
using Microsoft.AspNetCore.Mvc;
using Polly;

namespace AutomateThis.Controllers
{
	[ApiController]
	[Route("/v1/weather")]
	public class WeatherController
	{
		private readonly IOpenWeatherMapApi _api;
		private readonly IMapper _mapper;
		private readonly ConfigurationManager _config;
		private readonly IAsyncPolicy<Weather.Weather> _policy;

		public WeatherController(
			IOpenWeatherMapApi api,
			IMapper mapper,
			ConfigurationManager config,
			IAsyncPolicy<Weather.Weather> policy
		)
		{
			_api = api;
			_mapper = mapper;
			_config = config;
			_policy = policy;
		}

		[HttpGet]
		[Route("")]
		public async Task<ActionResult<Weather.Weather>> Index()
		{
			return await _policy.ExecuteAsync(async () =>
			{
				var response = await _api.GetCurrentAsync(
					_config["OpenWeatherMap:Location"] ?? throw new ArgumentNullException(),
					"metric",
					_config["OpenWeatherMap:ApiKey"] ?? throw new ArgumentNullException()
				);
				return _mapper.Map<Weather.Weather>(response.Content);
			});
		}
	}
}
