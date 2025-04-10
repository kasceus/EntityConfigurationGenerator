using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace EntityConfigurationGenerator
{
    internal sealed class ToggleUsePartialsCommand
    {

        private readonly AsyncPackage _Package;

        private ToggleUsePartialsCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this._Package = package ?? throw new ArgumentNullException(nameof(package));

            var menuCommandID = new CommandID(Guids.CommandSet, Guids.CommandIds.ToggleUsePartials);


            _ = Task.Run(async () =>
            {

                var menuItem = new OleMenuCommand(Execute, menuCommandID);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_Package.DisposalToken);
                menuItem.BeforeQueryStatus += UpdateCommandStatus;
                commandService.AddCommand(menuItem);
                menuItem.Visible = true;
            }); // new!

        }
        private void UpdateCommandStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            System.Diagnostics.Debug.WriteLine("BeforeQueryStatus fired");

            var settings = (EntityConfigOptionsPage)_Package.GetDialogPage(typeof(EntityConfigOptionsPage));

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

            var settings = (EntityConfigOptionsPage)_Package.GetDialogPage(typeof(EntityConfigOptionsPage));

            settings.GenerateAsPartial = !settings.GenerateAsPartial;
            if (sender is OleMenuCommand menuCommand)
            {
                menuCommand.Text = settings.GenerateAsPartial ? "Use Partials ✔" : "Use Partials ✖";
            }

            VsShellUtilities.ShowMessageBox(
                _Package,
                $"Generate as Partial is now {(settings.GenerateAsPartial ? "ON" : "OFF")}.",
                "Configuration Updated",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST
            );
        }
    }

}
