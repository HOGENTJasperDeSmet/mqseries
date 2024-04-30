using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IBM.XMS;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using RedisListener.Context;

namespace RedisListener.Listener
{
	internal class RedisListener : IListener
	{
		private readonly ITriggeredFunctionExecutor _triggeredFunctionExecutor;

		private readonly RedisTriggerContext _triggerContext;

		private CancellationTokenSource _cts;

		public RedisListener(
			ITriggeredFunctionExecutor triggeredFunctionExecutor,
			RedisTriggerContext triggerContext)
		{
			_triggeredFunctionExecutor = triggeredFunctionExecutor;
			_triggerContext = triggerContext;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_cts = new CancellationTokenSource();
			_ = RunRedisListener(_cts.Token);
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			Cancel();
			return Task.CompletedTask;
		}

		public void Cancel()
		{
			_cts.Cancel();
		}

		public void Dispose()
		{
			Cancel();
		}

		private async Task RunRedisListener(CancellationToken ct)
		{
            XMSFactoryFactory factoryFactory;
            IConnectionFactory cf;
            IDestination destination;
            IMessageConsumer consumerAsync;
            MessageListener messageListener;
            // Get an instance of factory.
            factoryFactory = XMSFactoryFactory.GetInstance(XMSC.CT_WMQ);

            // Create WMQ Connection Factory.
            cf = factoryFactory.CreateConnectionFactory();

            // Set the properties
            cf.SetStringProperty(XMSC.WMQ_HOST_NAME, "0.tcp.eu.ngrok.io");
            cf.SetIntProperty(XMSC.WMQ_PORT, 13123);
            cf.SetStringProperty(XMSC.WMQ_CHANNEL, "DEV.ADMIN.SVRCONN");
            cf.SetIntProperty(XMSC.WMQ_CONNECTION_MODE, XMSC.WMQ_CM_CLIENT);
            cf.SetStringProperty(XMSC.WMQ_QUEUE_MANAGER, "QM1");
            cf.SetStringProperty(XMSC.USERID, "admin");
            cf.SetStringProperty(XMSC.PASSWORD, "passw0rd");

            // Create connection.
            var connectionWMQ = cf.CreateConnection();
            // Create session with client acknowledge so that we can acknowledge 
            // only if message is sent to Azure Service Bus queue
            var sessionWMQ = connectionWMQ.CreateSession(false, AcknowledgeMode.AutoAcknowledge);
            // Create destination
            destination = sessionWMQ.CreateQueue("DEV.QUEUE.1");
            // Create consumer
            consumerAsync = sessionWMQ.CreateConsumer(destination);

            // Setup a message listener and assign it to consumer
            messageListener = new MessageListener(async x => {

                ITextMessage textMessage = (ITextMessage)x;


                await Task.WhenAll(_triggeredFunctionExecutor.TryExecuteAsync(new TriggeredFunctionData
                {
                    TriggerValue = JsonSerializer.Serialize(textMessage)
                }, ct));
            });
            consumerAsync.MessageListener = messageListener;

            // Start the connection to receive messages.
            connectionWMQ.Start();

            // Wait for messages till a key is pressed by user

            // Cleanup



            while (!ct.IsCancellationRequested)
			{
				try
				{
			

					await Task.Delay(500, ct);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			}
            consumerAsync.Close();
            destination.Dispose();
            sessionWMQ.Dispose();
            connectionWMQ.Close();
        }
	}
}