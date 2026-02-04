using Azure.Messaging;
using Azure;
using Azure.Messaging.EventGrid.Namespaces;


// TODO: Replace the following placeholders with appropriate values

// Endpoint of the namespace that you can find on the Overview page for your Event Grid namespace. Prefix it with https://.
// Should be in the form: https://namespace01.eastus-1.eventgrid.azure.net. 
var namespaceEndpoint = "Event grid end point";
//var namespaceEndpoint = "https://testanneventnamespace.eastus-1.eventgrid.azure.net";

// Name of the topic in the namespace
//var topicName = "<TOPIC-NAME>";
var topicName = "topic01";

// Access key for the topic
//var topicKey = "<TOPIC-ACCESS-KEY>";
var topicKey = "Event grid topic key";


// Construct the client using an Endpoint for a namespace as well as the access key
var client = new EventGridSenderClient(new Uri(namespaceEndpoint), topicName, new AzureKeyCredential(topicKey));

// Publish a single CloudEvent using a custom TestModel for the event data.
var @ev = new CloudEvent("employee_source", "type", new TestModel { Name = "Raj", Age = 18 });
await client.SendAsync(ev);

// Publish a batch of CloudEvents.

await client.SendAsync(
new[] {
    new CloudEvent("employee_source", "type", new TestModel { Name = "Jai", Age = 55 }),
    new CloudEvent("employee_source", "type", new TestModel { Name = "Nisha", Age = 25 })});

Console.WriteLine("Three events have been published to the topic. Press any key to end the application.");
Console.ReadKey();

public class TestModel
{
    public string Name { get; set; }
    public int Age { get; set; }
}


