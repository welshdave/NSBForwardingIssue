using NServiceBus;
using System;
using System.Threading.Tasks;

namespace NserviceBusForward
{
    public class Program
    {
        private static string connectionString = "Endpoint=sb://[your namespace].servicebus.windows.net;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=[your secret]";
        private const string GatewayEndpoint = "http://localhost:24873/NServiceBusIssueEndpoint/";
        public static async Task Main(string[] args)
        {
            var endpointConfiguration = new EndpointConfiguration("NServiceBusIssueEndpoint");
            
            var transport = endpointConfiguration.UseTransport<AzureServiceBusTransport>();
            transport.UseForwardingTopology();
            transport.ConnectionString(connectionString);

            var queues = transport.Queues();
            queues.ForwardDeadLetteredMessagesTo(
                condition: queueName => { return queueName != "error"; },
                forwardDeadLetteredMessagesTo: ("error")
                );

            var subscriptions = transport.Subscriptions();
            subscriptions.ForwardDeadLetteredMessagesTo("error");

            endpointConfiguration.UseSerialization<NewtonsoftSerializer>();
            endpointConfiguration.UsePersistence<AzureStoragePersistence>();
            endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.GatewayDeduplication>();
            endpointConfiguration.EnableInstallers();
            var gateway = endpointConfiguration.Gateway();
            gateway.AddReceiveChannel(GatewayEndpoint, "Http", isDefault: true);

            var nsb = await StartBus(endpointConfiguration);

            Console.WriteLine($"Gateway running at {GatewayEndpoint}. \nPress any key to stop.");
            Console.ReadKey();

            await nsb.Stop();
        }

        static async Task<IEndpointInstance> StartBus(EndpointConfiguration endpointConfiguration)
        {
            var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);
            return endpointInstance;
        }
    }
}
