namespace Microsoft.Extensions.Configuration;

public sealed record ConnectionStringInfo(
	string Name,
	string Value
) {
	public bool IsAzureSql => Value.ContainsAny([".database.windows.net"]);
	public bool IsAzureMonitor => Value.ContainsAny([".applicationinsights.azure.com", ".monitor.azure.com"]);
	public bool IsAzureAIFoundry => Value.ContainsAny([".services.ai.azure.com"]);
	public bool IsAzureAIInference => Value.ContainsAny([".inference.ai.azure.com"]);
	public bool IsAzureOpenAI => Value.ContainsAny([".openai.azure.com"]);

	public static implicit operator string(ConnectionStringInfo x) => x.Value;
}
