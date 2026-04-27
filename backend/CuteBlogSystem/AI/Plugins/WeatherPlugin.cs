using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace CuteBlogSystem.AI.Plugins
{
    public class WeatherPlugin
    {
        [KernelFunction]
        [Description("获取指定城市的当前天气")]
        public string GetWeather(
            [Description("城市名称")] string city)
        {
            // 模拟数据（先不用真实API）
            return $"{city} 当前晴朗，气温 25°C";
        }
    }
}