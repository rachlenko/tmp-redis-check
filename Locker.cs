
using Polly.Retry;
using Polly;
using StackExchange.Redis;

namespace MemoryTester
{

    public interface ILocker
    {
        Task<bool> TrySet(string key);

        Task Delete(string key);
    }


    public class Locker : ILocker
    {
        private string _env;
        private IConnectionMultiplexer _connection;
        private ResiliencePipeline _pipeline;

        public Locker(IConnectionMultiplexer connection,IConfiguration configuration)
        {
            _connection = connection;
            _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions { MaxRetryAttempts=3,Delay=TimeSpan.FromMilliseconds(10)})
            .Build();
        }
        public async Task<bool> TrySet(string key)
        {
            var res = await _pipeline.ExecuteAsync(async (c) =>
            {
                var db = _connection.GetDatabase();
                return await db.LockTakeAsync(key, key, TimeSpan.FromSeconds(10));
            });

            return res;
        }

        public async Task Delete(string key)
        {
            await _pipeline.ExecuteAsync(async (c) =>
            {
                var db = _connection.GetDatabase();
                var res = await db.LockReleaseAsync(key, key);
            });
        }


    }
}
