namespace Microsoft.Extensions.Configuration;

public static class ConfigurationExtensions {
	extension(IConfiguration config) {
		/// <summary>
		/// Gets the required connection string from the specified configuration.
		/// Shorthand for <c>GetSection("ConnectionStrings")[name] ?? throw new InvalidOperationException(...)</c>.
		/// </summary>
		/// <param name="name">The connection string key.</param>
		/// <returns>The connection string.</returns>
		public string GetRequiredConnectionString(string name) {
			ArgumentException.ThrowIfNullOrWhiteSpace(name);

			var value = config.GetConnectionString(name);
			if (string.IsNullOrWhiteSpace(value)) {
				throw new InvalidOperationException($"Connection string '{name}' is required.");
			}

			return value;
		}

		/// <summary>
		/// Gets the connection string information from the specified configuration.
		/// </summary>
		/// <param name="name">The connection string key.</param>
		/// <returns>The connection string information.</returns>
		public ConnectionStringInfo? GetConnectionStringInfo(string name) {
			ArgumentException.ThrowIfNullOrWhiteSpace(name);

			var value = config.GetConnectionString(name);
			if (string.IsNullOrWhiteSpace(value)) {
				return null;
			}

			return new(name, value);
		}

		/// <summary>
		/// Gets the required connection string information from the specified configuration.
		/// </summary>
		/// <param name="name">The connection string key.</param>
		/// <returns>The connection string information.</returns>
		public ConnectionStringInfo GetRequiredConnectionStringInfo(string name) {
			ArgumentException.ThrowIfNullOrWhiteSpace(name);

			var value = config.GetRequiredConnectionString(name);
			return new(name, value);
		}
	}
}