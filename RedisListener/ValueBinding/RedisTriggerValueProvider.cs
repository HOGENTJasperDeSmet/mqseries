﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using RedisListener.Common;
using StackExchange.Redis;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace RedisListener.ValueBinding
{
	internal class RedisTriggerValueProvider : IValueProvider
	{
		public Type Type { get; }

		private readonly String _streamEntry;

		private readonly JsonSerializerOptions _options;

		public RedisTriggerValueProvider(String streamEntry, Type requestedParameterType)
		{
			Type = requestedParameterType;
			_streamEntry = streamEntry;

		}

		public Task<object> GetValueAsync() => Task.FromResult<object>(ToInvokeString());

		public string ToInvokeString()
		{
			return JsonSerializer.Serialize(_streamEntry, _options);
		}
	}
}