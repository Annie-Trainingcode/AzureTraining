using Azure;
using Azure.Messaging;
using Azure.Messaging.EventGrid.Namespaces;

// TODO: Replace the following placeholders with appropriate values

// Endpoint of the namespace that you can find on the Overview page for your Event Grid namespace
// Example: https://namespace01.eastus-1.eventgrid.azure.net. 
//var namespaceEndpoint = "<NAMESPACE-ENDPOINT>"; // Should be in the form: https://namespace01.eastus-1.eventgrid.azure.net. 
var namespaceEndpoint = "Event grid endpoint";
// Name of the topic in the namespace
//var topicName = "<TOPIC-NAME>";
var topicName = "topic01";

// Access key for the topic
//var topicKey = "<TOPIC-ACCESS-KEY>";
var topicKey = "Event grid topic key";

// Name of the subscription to the topic
//var subscriptionName = "<TOPIC-SUBSCRIPTION-NAME>";
var subscriptionName = "subscriber02";


// Maximum number of events you want to receive
const short MaxEventCount = 3;

// Construct the client using an Endpoint for a namespace as well as the access key
var client = new EventGridReceiverClient(new Uri(namespaceEndpoint), topicName, subscriptionName, new AzureKeyCredential(topicKey));

// Receive the published CloudEvents. 
ReceiveResult result = await client.ReceiveAsync(MaxEventCount);

Console.WriteLine("Received Response");
Console.WriteLine("-----------------");
// handle received messages. Define these variables on the top.

var toRelease = new List<string>();
var toAcknowledge = new List<string>();
var toReject = new List<string>();

// Iterate through the results and collect the lock tokens for events we want to release/acknowledge/result

foreach (ReceiveDetails detail in result.Details)
{
    CloudEvent @event = detail.Event;
    BrokerProperties brokerProperties = detail.BrokerProperties;
    Console.WriteLine(@event.Data.ToString());

    // The lock token is used to acknowledge, reject or release the event
    Console.WriteLine(brokerProperties.LockToken);
    Console.WriteLine();

    // If the event is from the "employee_source" and the name is "Bob", we are not able to acknowledge it yet, so we release it
    if (@event.Source == "employee_source" && @event.Data.ToObjectFromJson<TestModel>().Name == "Bob")
    {
        toRelease.Add(brokerProperties.LockToken);
    }
    // acknowledge other employee_source events
    else if (@event.Source == "employee_source")
    {
        toAcknowledge.Add(brokerProperties.LockToken);
    }
    // reject all other events
    else
    {
        toReject.Add(brokerProperties.LockToken);
    }
}

// Release/acknowledge/reject the events

if (toRelease.Count > 0)
{
    ReleaseResult releaseResult = await client.ReleaseAsync(toRelease);

    // Inspect the Release result
    Console.WriteLine($"Failed count for Release: {releaseResult.FailedLockTokens.Count}");
    foreach (FailedLockToken failedLockToken in releaseResult.FailedLockTokens)
    {
        Console.WriteLine($"Lock Token: {failedLockToken.LockToken}");
        Console.WriteLine($"Error Code: {failedLockToken.Error}");
        Console.WriteLine($"Error Description: {failedLockToken.ToString}");
    }

    Console.WriteLine($"Success count for Release: {releaseResult.SucceededLockTokens.Count}");
    foreach (string lockToken in releaseResult.SucceededLockTokens)
    {
        Console.WriteLine($"Lock Token: {lockToken}");
    }
    Console.WriteLine();
}

if (toAcknowledge.Count > 0)
{
    AcknowledgeResult acknowledgeResult = await client.AcknowledgeAsync(toAcknowledge);

    // Inspect the Acknowledge result
    Console.WriteLine($"Failed count for Acknowledge: {acknowledgeResult.FailedLockTokens.Count}");
    foreach (FailedLockToken failedLockToken in acknowledgeResult.FailedLockTokens)
    {
        Console.WriteLine($"Lock Token: {failedLockToken.LockToken}");
        Console.WriteLine($"Error Code: {failedLockToken.Error}");
        Console.WriteLine($"Error Description: {failedLockToken.ToString}");
    }

    Console.WriteLine($"Success count for Acknowledge: {acknowledgeResult.SucceededLockTokens.Count}");
    foreach (string lockToken in acknowledgeResult.SucceededLockTokens)
    {
        Console.WriteLine($"Lock Token: {lockToken}");
    }
    Console.WriteLine();
}

if (toReject.Count > 0)
{
    RejectResult rejectResult = await client.RejectAsync(toReject);

    // Inspect the Reject result
    Console.WriteLine($"Failed count for Reject: {rejectResult.FailedLockTokens.Count}");
    foreach (FailedLockToken failedLockToken in rejectResult.FailedLockTokens)
    {
        Console.WriteLine($"Lock Token: {failedLockToken.LockToken}");
        Console.WriteLine($"Error Code: {failedLockToken.Error}");
        Console.WriteLine($"Error Description: {failedLockToken.ToString}");
    }

    Console.WriteLine($"Success count for Reject: {rejectResult.SucceededLockTokens.Count}");
    foreach (string lockToken in rejectResult.SucceededLockTokens)
    {
        Console.WriteLine($"Lock Token: {lockToken}");
    }
    Console.WriteLine();
}

public class TestModel
{
    public string Name { get; set; }
    public int Age { get; set; }
}

