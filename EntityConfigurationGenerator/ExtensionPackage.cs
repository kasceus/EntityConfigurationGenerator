using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;


namespace EntityConfigurationGenerator
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(ExtensionPackage.PackageGuidString)]
    [ProvideOptionPage(typeof(EntityConfigOptionsPage), "Entity Config Generator", "Settings", 0, 0, true)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]

    public sealed class ExtensionPackage : AsyncPackage
    {
        public const string PackageGuidString = Constants.PackageGuidString;
        public ExtensionPackage()
        {
        }
        #region Package Members
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            try
            {
                await ToggleUsePartialsCommand.InitializeAsync(this);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            await GenerateSingleConfigurationCommand.InitializeAsync(this);
            await GenerateAllConfigurationsCommand.InitializeAsync(this);
            //register the options page events
            var optionsPage = (EntityConfigOptionsPage)GetDialogPage(typeof(EntityConfigOptionsPage));
            if (optionsPage != null)
            {
                optionsPage.GenerateAsPartialChanged += (s, e) =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    UpdateUI(optionsPage.GenerateAsPartial);
                };
            }
        }

        private void UpdateUI(bool generateAsPartial)
        {
            UpdateButtonText(generateAsPartial);

            // Optionally, update status bar or other UI elements
            IVsStatusbar statusBar = (IVsStatusbar)GetService(typeof(SVsStatusbar));
            if (statusBar != null)
            {
                statusBar.SetText(generateAsPartial ? "Partials Enabled" : "Partials Disabled");
            }
        }
        private void UpdateButtonText(bool isEnabled)
        {
            OleMenuCommandService commandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var commandId = new CommandID(new Guid("33b9a065-3e45-4f15-8c28-cfb1303db805"), 0x0012); // Replace with your actual GUID and ID
                var menuCommand = commandService.FindCommand(commandId) as OleMenuCommand;

                if (menuCommand != null)
                {
                    menuCommand.Text = isEnabled ? "Turn Off Partials ✖" : "Turn On Partials ✔";
                    menuCommand.Checked = isEnabled;
                }
            }
        }
        #endregion
    }
}
