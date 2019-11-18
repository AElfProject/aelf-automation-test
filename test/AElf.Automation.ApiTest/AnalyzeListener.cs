using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace AElf.Automation.ApiTest
{
    public class AnalyzeListener
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private Stopwatch _stopwatch;

        public AnalyzeListener(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public async Task<(T, long)> ExecuteApi<T>(Func<Task<T>> task) where T : new()
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            T result = default;
            try
            {
                result = await task.Invoke();
            }
            catch (Exception e)
            {
                _testOutputHelper.WriteLine($"Exception: {e.Message}");
            }
            finally
            {
                _stopwatch.Stop();
            }

            var timeSpan = _stopwatch.ElapsedMilliseconds;

            return (result, timeSpan);
        }

        public async Task<(T, long)> ExecuteApi<T>(Func<object[], Task<T>> task, params object[] parameterArray)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            var result = default(T);
            try
            {
                result = await task.Invoke(parameterArray);
            }
            catch (Exception e)
            {
                _testOutputHelper.WriteLine($"Exception: {e.Message}");
            }
            finally
            {
                _stopwatch.Stop();
            }

            var timeSpan = _stopwatch.ElapsedMilliseconds;
            return (result, timeSpan);
        }
    }
}