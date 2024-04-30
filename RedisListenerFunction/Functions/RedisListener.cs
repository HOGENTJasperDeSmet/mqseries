using Microsoft.Azure.Functions.Worker;
using RedisListener.WorkerBinding;
using StackExchange.Redis;

namespace RedisListenerFunction.Functions
{
	public static class RedisListener
	{
		[Function("RedisConsumer")]
		public static void HandleRedisMessage([RedisTrigger("localhost:5789", "test1234")] string entry)
		{
			Console.WriteLine($"{entry}");
		}
	}
}