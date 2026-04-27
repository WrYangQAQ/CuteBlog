using Microsoft.SemanticKernel;
using System.Text.Json;

namespace CuteBlogSystem.AI.Filters
{
    public class FunctionInvocationLoggingFilter : IFunctionInvocationFilter
    {
        private readonly ILogger<FunctionInvocationLoggingFilter> _logger;

        public FunctionInvocationLoggingFilter(ILogger<FunctionInvocationLoggingFilter> logger)
        {
            _logger = logger;
        }

        public async Task OnFunctionInvocationAsync(
            FunctionInvocationContext context,
            Func<FunctionInvocationContext, Task> next)
        {
            _logger.LogInformation(
                "模型准备调用函数：{PluginName}.{FunctionName}，参数：{Arguments}",
                context.Function.PluginName,
                context.Function.Name,
                JsonSerializer.Serialize(context.Arguments));

            await next(context);

            _logger.LogInformation(
                "函数调用完成：{PluginName}.{FunctionName}",
                context.Function.PluginName,
                context.Function.Name);
        }
    }
}