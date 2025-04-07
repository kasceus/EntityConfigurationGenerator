using EnvDTE;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Task = System.Threading.Tasks.Task;

namespace EntityConfigurationGenerator
{
	/// <summary>
	/// Command handler
	/// </summary>
	internal sealed class GenerateAllConfigurationsCommand
	{
		private async Task<IVsOutputWindowPane> GetOutputPaneAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			IVsOutputWindow outputWindow = (IVsOutputWindow)await _Package.GetServiceAsync(typeof(SVsOutputWindow)) ?? throw new Exception("problem getting service");
			Guid paneGuid = new Guid("e1d890f1-d191-45e2-9ad9-bfcf993003c1"); // your own unique GUID
			string title = "Entity Configuration Generator";

			outputWindow.CreatePane(ref paneGuid, title, fInitVisible: 1, fClearWithSolution: 1);
			outputWindow.GetPane(ref paneGuid, out IVsOutputWindowPane pane);

			return pane;
		}

		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x0101;

		/// <summary>
		/// Command menu group (command set GUID).
		/// </summary>
		public static readonly Guid CommandSet = new Guid("7fb0260a-1b80-4015-9025-7c9571af8c59");

		/// <summary>
		/// VS Package that provides this command, not null.
		/// </summary>
		private readonly AsyncPackage _Package;

		/// <summary>
		/// Initializes a new instance of the <see cref="GenerateAllConfigurationsCommand"/> class.
		/// Adds our command handlers for menu (commands must exist in the command table file)
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		/// <param name="commandService">Command service to add command to, not null.</param>
		private GenerateAllConfigurationsCommand(AsyncPackage package, OleMenuCommandService commandService)
		{
			this._Package = package ?? throw new ArgumentNullException(nameof(package));
			commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

			var menuCommandID = new CommandID(CommandSet, CommandId);
			var menuItem = new MenuCommand(this.Execute, menuCommandID);
			commandService.AddCommand(menuItem);
		}

		/// <summary>
		/// Gets the instance of the command.
		/// </summary>
		public static GenerateAllConfigurationsCommand Instance
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the service provider from the owner package.
		/// </summary>
		private Microsoft.VisualStudio.Shell.IAsyncServiceProvider _ServiceProvider
		{
			get
			{
				return this._Package;
			}
		}

		/// <summary>
		/// Initializes the singleton instance of the command.
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		public static async Task InitializeAsync(AsyncPackage package)
		{
			// Switch to the main thread - the call to AddCommand in GenerateAllConfigurationsCommand's constructor requires
			// the UI thread.
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
			Instance = new GenerateAllConfigurationsCommand(package, commandService);
		}

		/// <summary>
		/// This function is the callback used to execute the command when the menu item is clicked.
		/// See the constructor to see how the menu item is associated with this function using
		/// OleMenuCommandService service and MenuCommand class.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="e">Event args.</param>
		private async void Execute(object sender, EventArgs e)
		{
			var outputPane = await GetOutputPaneAsync();
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			outputPane.OutputStringThreadSafe("Starting configuration generation...\n");
			outputPane.Activate(); // Bring the pane to front
			var dte = (DTE)Package.GetGlobalService(typeof(DTE));
			var selectedItem = dte.SelectedItems.Item(1);

			string folderPath = selectedItem.ProjectItem?.Properties?.Item("FullPath")?.Value as string;
			if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
				return;

			// Load settings
			var settings = (EntityConfigOptionsPage)((AsyncPackage)_Package).GetDialogPage(typeof(EntityConfigOptionsPage));
			bool generateAsPartial = settings.GenerateAsPartial;

			var parentDir = Directory.GetParent(folderPath)?.FullName;
			if (parentDir == null) return;

			string configDir = Path.Combine(parentDir, "Configurations");
			Directory.CreateDirectory(configDir);

			var csFiles = Directory.GetFiles(folderPath, "*.cs");
			foreach (var file in csFiles)
			{
				if (Path.GetFileName(file).StartsWith("Configurations", StringComparison.OrdinalIgnoreCase))
					continue; // Skip if it's already in the Configurations folder
				await Task.Yield(); // Yield to avoid blocking the UI thread

				string className = Path.GetFileNameWithoutExtension(file);
				outputPane.OutputStringThreadSafe($"  Generating config for: {className}\n");
				string[] originalLines = File.ReadAllLines(file);
				string originalNamespace = originalLines.FirstOrDefault(l => l.TrimStart().StartsWith("namespace"))?
					.Trim().Replace("namespace ", "").TrimEnd(';') ?? string.Empty;
				string configPath = Path.Combine(configDir, $"{className}Configuration.cs");

				if (generateAsPartial)
				{
					outputPane.OutputStringThreadSafe("Generating the new config class as a partial.\n");

					string fileContent = File.ReadAllText(file);
					if (!fileContent.Contains("partial class"))
					{
						fileContent = Regex.Replace(
							fileContent,
							@"\bpublic\s+class\s+(\w+)",
							"public partial class $1"
						);
						File.WriteAllText(file, fileContent);
						outputPane.OutputStringThreadSafe($"  Updated {className} to partial class.\n");

					}
				}

				if (File.Exists(configPath)) continue;

				string partialKeyword = generateAsPartial ? "partial " : "";
				string usingEntityNamespace = generateAsPartial == false ? (string.IsNullOrWhiteSpace(originalNamespace) ? "" : $"using {originalNamespace};{Environment.NewLine}") : ""; // add the new namespace if it's generated in the configurations directory


				// Compute target namespace
				string fileNamespace = generateAsPartial
					? originalNamespace // keep it in same namespace if partial
					: originalNamespace.Replace(".Models", ".Configurations");
				string template = $@"using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
{usingEntityNamespace}
namespace {fileNamespace}
{{
    public {partialKeyword}class {className}Configuration : IEntityTypeConfiguration<{className}>
    {{
        public void Configure(EntityTypeBuilder<{className}> builder)
        {{
        }}
    }}
}}";

				File.WriteAllText(configPath, template);
				ProjectItem selectedFolder = selectedItem.ProjectItem;

				// Step 1: Get the sibling collection (this is the children of DataEase)
				ProjectItems siblingItems = selectedFolder.Collection;

				// Step 2: Add or find Configurations in the same level as Models
				ProjectItem configFolderItem = siblingItems
					.Cast<ProjectItem>()
					.FirstOrDefault(i =>
					{
						Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
						return i.Name == "Configurations";
					})
					?? siblingItems.AddFolder("Configurations");

				// Step 3: Add the generated file
				configFolderItem.ProjectItems.AddFromFile(configPath);
			}

			VsShellUtilities.ShowMessageBox(
				this._Package,
				"Configurations generated.",
				"Success",
				OLEMSGICON.OLEMSGICON_INFO,
				OLEMSGBUTTON.OLEMSGBUTTON_OK,
				OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
			outputPane.OutputStringThreadSafe("Done.\n");

		}
		private IEnumerable<ProjectItem> GetAllProjectItemsRecursive(ProjectItem item)
		{
			yield return item;

			if (item.ProjectItems != null)
			{
				foreach (ProjectItem subItem in item.ProjectItems)
				{
					foreach (var child in GetAllProjectItemsRecursive(subItem))
						yield return child;
				}
			}
		}
	}
}
