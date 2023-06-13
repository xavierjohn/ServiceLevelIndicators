namespace ServiceLevelIndicators
{
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.DependencyInjection;

    public class ServiceLevelIndicatorLatencyAttribute : ActionFilterAttribute
    {
        public string Operation { get; set; } = string.Empty;
        public const string MeasureOperationLatencyOperationLabel = "MeasureOperationLatency";

        private LatencyMeasureOperation? _measuredOperation;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var serviceLevelIndicator = context.HttpContext.RequestServices.GetRequiredService<ServiceLevelIndicator>();
            if (Operation == string.Empty)
                Operation = context.HttpContext.Request.Method + " " + context.ActionDescriptor.AttributeRouteInfo.Template;

            _measuredOperation = serviceLevelIndicator.StartLatencyMeasureOperation(Operation);
            RemoveParentOperationIfAny(context);
            context.HttpContext.Items[MeasureOperationLatencyOperationLabel] = _measuredOperation;
        }

        private static void RemoveParentOperationIfAny(ActionExecutingContext context)
        {
            if (context.HttpContext.Items.TryGetValue(MeasureOperationLatencyOperationLabel, out var obj))
            {
                if (obj is LatencyMeasureOperation oldOperation)
                    oldOperation.DoEmitMetrics = false;
            }
        }

        public override void OnResultExecuted(ResultExecutedContext context)
        {
            if (_measuredOperation != null)
            {
                var statusCode = context.HttpContext.Response.StatusCode;
                _measuredOperation.SetHttpStatusCode(statusCode);
                _measuredOperation.SetState((statusCode >= 200 && statusCode < 300) ? System.Diagnostics.ActivityStatusCode.Ok : System.Diagnostics.ActivityStatusCode.Error);
                _measuredOperation.Dispose();
            }
        }
    }
}
