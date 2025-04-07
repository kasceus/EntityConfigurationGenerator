using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace EntityConfigurationGenerator
{
    internal sealed class ToggleUsePartialsCommand
    {
        public const int CommandId = 0x0102; // Unique ID for the command
        public static readonly Guid CommandSet = new Guid("d309f791-903f-11d0-9efc-00a0c911004f"); // Replace with your actual GUID

        private readonly AsyncPackage package;

        private ToggleUsePartialsCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));

            var menuCommandID = new CommandID(CommandSet, CommandId);

            var menuItem = new OleMenuCommand(Execute, menuCommandID);
            menuItem.BeforeQueryStatus += UpdateCommandStatus; // new!
            commandService.AddCommand(menuItem);
        }
        private void UpdateCommandStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var settings = (EntityConfigOptionsPage)package.GetDialogPage(typeof(EntityConfigOptionsPage));

            if (sender is OleMenuCommand menuCommand)
            {
                menuCommand.Text = settings.GenerateAsPartial ? "Use Partials ✔" : "Use Partials ✖";
                menuCommand.Checked = settings.GenerateAsPartial;
                menuCommand.Visible = true;
                menuCommand.Enabled = true;
            }
        }
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {

                new ToggleUsePartialsCommand(package, commandService);
            }

        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var settings = (EntityConfigOptionsPage)package.GetDialogPage(typeof(EntityConfigOptionsPage));

            settings.GenerateAsPartial = !settings.GenerateAsPartial;
            if (sender is OleMenuCommand menuCommand)
            {
                menuCommand.Text = settings.GenerateAsPartial ? "Use Partials ✔" : "Use Partials ✖";
            }

            VsShellUtilities.ShowMessageBox(
                package,
                $"Generate as Partial is now {(settings.GenerateAsPartial ? "ON" : "OFF")}.",
                "Configuration Updated",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST
            );
        }
    }

}
