using Core.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Api
{
    public class AggregationIdMiddleware
    {
		public const string AggregationIdContextItem = "__TCP_CUSTOM_AGGREGATION_ID";

		private readonly RequestDelegate _next;

		public AggregationIdMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task InvokeAsync(
			HttpContext context,
			IOptions<WebApiOptions> options,
			ILogger<AggregationIdMiddleware> logger)
		{
			var headerKey = options.Value.AggregationIdHeader ?? "X-Request-ID";
			var aggregationIdValue = $"request-{Guid.NewGuid()}";
			if (context.Request.Headers.ContainsKey(headerKey))
			{
				aggregationIdValue = context.Request.Headers[headerKey].FirstOrDefault()
									 ?? aggregationIdValue;
			}

			using (Aggregator.CreateAggregationContext(aggregationIdValue))
			{
				logger.LogInformation(DefaultLoggingEvents.AggregateIdGenerated, "Aggregate Id Generated: {AggregateId}", Aggregator.CurrentAggregationId);
				await _next(context);
			}
		}
	}
}
