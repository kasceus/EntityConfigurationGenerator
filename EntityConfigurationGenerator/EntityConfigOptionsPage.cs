using Microsoft.VisualStudio.Shell;

using System.ComponentModel;

namespace EntityConfigurationGenerator
{
	public class EntityConfigOptionsPage : DialogPage
	{
		[Category("Generation Settings")]
		[DisplayName("Generate as Partial")]
		[Description("Generate configuration class as a partial and update the original class.")]
		public bool GenerateAsPartial { get; set; } = false;
	}
}
