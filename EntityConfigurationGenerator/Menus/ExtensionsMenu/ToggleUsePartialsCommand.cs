using EntityConfigurationGenerator.Menus.ExtensionsMenu;

using Microsoft.VisualStudio.Shell;

using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace EntityConfigurationGenerator
{
    internal sealed class ToggleUsePartialsCommand
    {
        private readonly OleMenuCommand _MenuItem;
        private readonly AsyncPackage _Package;

        private ToggleUsePartialsCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this._Package = package ?? throw new ArgumentNullException(nameof(package));

            var menuCommandID = new CommandID(MenuConstants._CommandSet, MenuConstants._ToggleUsePartialsCommandId);

            var menuItem = new OleMenuCommand(Execute, menuCommandID);
            menuItem.BeforeQueryStatus += UpdateCommandStatus;
            commandService.AddCommand(menuItem);
        }
        private void UpdateCommandStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var settings = (EntityConfigOptionsPage)_Package.GetDialogPage(typeof(EntityConfigOptionsPage));

            if (sender is OleMenuCommand menuCommand)
            {
                menuCommand.Text = settings.GenerateAsPartial ? "Disable Partials ✖" : "Enable Partials ✔";
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
                var d = new ToggleUsePartialsCommand(package, commandService);
            }

        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var settings = (EntityConfigOptionsPage)_Package.GetDialogPage(typeof(EntityConfigOptionsPage));

            settings.GenerateAsPartial = !settings.GenerateAsPartial;
            if (sender is OleMenuCommand menuCommand)
            {
                menuCommand.Text = settings.GenerateAsPartial ? "Disable Partials ✖" : "Enable Partials ✔";
                ;
                menuCommand.Checked = settings.GenerateAsPartial;
            }
        }
    }

}
