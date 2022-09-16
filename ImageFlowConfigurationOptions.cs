namespace ImageServer
{
	public class ImageFlowConfigurationOption
    {
		public const string ImageFlow = "ImageFlow";

		public string CacheDirectory { get; set; } = String.Empty;
		public string CacheMaxAge { get; set; } = String.Empty;
		public int CacheSize { get; set; } = 0;
		public string SignatureKey { get; set; } = String.Empty;

		public string DiagnosticPassword { get; set; } = String.Empty;
        public List<PresetConfigurationOption> Presets { get; set; } = new List<PresetConfigurationOption>();

	}

	public class PresetConfigurationOption
	{
		public string Name { get; set; } = String.Empty;
		public List<CommandConfigurationOption> Commands { get; set; } = new List<CommandConfigurationOption>();

	}

	public class CommandConfigurationOption
	{ 
		public string Name { get; set; } = String.Empty;
		public string Value { get; set; } = String.Empty;
	}
}
