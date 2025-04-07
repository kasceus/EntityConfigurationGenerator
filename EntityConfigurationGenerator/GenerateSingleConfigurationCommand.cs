using EnvDTE;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using System;
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
    internal sealed class GenerateSingleConfigurationCommand
    {
        private async Task<IVsOutputWindowPane> GetOutputPaneAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var outputWindow = (IVsOutputWindow)await _Package.GetServiceAsync(typeof(SVsOutputWindow));
            Guid paneGuid = new Guid("e1d890f1-d191-45e2-9ad9-bfcf993003c1"); // your own unique GUID
            string title = "Entity Configuration Generator";

            outputWindow.CreatePane(ref paneGuid, title, fInitVisible: 1, fClearWithSolution: 1);
            outputWindow.GetPane(ref paneGuid, out IVsOutputWindowPane pane);

            return pane;
        }
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("7fb0260a-1b80-4015-9025-7c9571af8c59");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage _Package;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateSingleConfigurationCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private GenerateSingleConfigurationCommand(AsyncPackage package, OleMenuCommandService commandService)
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
        public static GenerateSingleConfigurationCommand Instance
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
            // Switch to the main thread - the call to AddCommand in GenerateEntityConfigurationCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new GenerateSingleConfigurationCommand(package, commandService);
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
            if (dte.SelectedItems.Count != 1) return;

            var selectedItem = dte.SelectedItems.Item(1);
            var filePath = selectedItem.ProjectItem.FileNames[1];

            var className = Path.GetFileNameWithoutExtension(filePath);
            outputPane.OutputStringThreadSafe($"  Generating config for: {className}\n");
            var modelDirectory = Path.GetDirectoryName(filePath);
            if (modelDirectory == null) return;

            var settings = (EntityConfigOptionsPage)((AsyncPackage)_Package).GetDialogPage(typeof(EntityConfigOptionsPage));
            bool generateAsPartial = settings.GenerateAsPartial;

            var parentDirectory = Directory.GetParent(modelDirectory)?.FullName;
            if (parentDirectory == null) return;

            var configDirectory = Path.Combine(parentDirectory, "Configurations");
            if (generateAsPartial)
            {
                // Ensure no unnecessary folder creation
                configDirectory = modelDirectory;
            }
            else
            {
                // Adjust path to prevent incorrect folder creation
                configDirectory = Path.Combine(parentDirectory, "Configurations");
                if (!Directory.Exists(configDirectory))
                {
                    outputPane.OutputStringThreadSafe($"Creating config directory: {configDirectory}\n");
                    Directory.CreateDirectory(configDirectory);
                }
            }

            string fileName = generateAsPartial
                                ? string.Format("{0}.config.cs", className)
                                : string.Format("{0}Configuration.cs", className);

            var configPath = Path.Combine(configDirectory, $"{fileName}");



            // Modify the source class to partial if needed
            if (generateAsPartial)
            {
                outputPane.OutputStringThreadSafe("Generating the new config class as a partial.\n");
                string fileContent = File.ReadAllText(filePath);
                if (!fileContent.Contains("partial class"))
                {
                    fileContent = Regex.Replace(
                        fileContent,
                        @"\bpublic\s+class\s+(\w+)",
                        "public partial class $1"
                    );
                    File.WriteAllText(filePath, fileContent);
                }
                outputPane.OutputStringThreadSafe($"  Updated {className} to partial class.\n");

            }

            // Get namespace from original file
            string[] originalLines = File.ReadAllLines(filePath);
            string originalNamespace = originalLines.FirstOrDefault(l => l.TrimStart().StartsWith("namespace"))?
                .Trim().Replace("namespace ", "").TrimEnd(';') ?? string.Empty;

            if (File.Exists(configPath))
            {
                VsShellUtilities.ShowMessageBox(
                    this._Package,
                    "Configuration file already exists.",
                    "Info",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            string partialKeyword = generateAsPartial ? "partial " : "";
            string usingEntityNamespace = generateAsPartial ? "" : $"using {originalNamespace};{Environment.NewLine}";

            var lastName = originalNamespace.Split('.').Last();
            string fileNamespace = generateAsPartial
                    ? originalNamespace // keep it in same namespace if partial
                    : originalNamespace.Replace(lastName, "Configurations");

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



            // Open the new file in the editor
            Window window = dte.ItemOperations.OpenFile(configPath);
            window.Visible = true;
            outputPane.OutputStringThreadSafe("Done.\n");
        }

    }
}
