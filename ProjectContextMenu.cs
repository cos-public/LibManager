using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.VCProjectEngine;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using System.Collections.Generic;
using EnvDTE80;
using System.Xml;
using System.Diagnostics;
using System.Windows;

namespace LibManager
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ProjectContextMenu
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("ec2f1a49-a0e8-46a7-a41b-0ec16ab76f2a");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly LibManagerPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectContextMenu"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private ProjectContextMenu(LibManagerPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ProjectContextMenu Instance { get; private set; }


        private LibManagerOptions _options;

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        public static async Task InitializeAsync(LibManagerPackage package)
        {
            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new ProjectContextMenu(package, commandService);
            Instance._options = package.Options;
        }

        public static Project GetSelectedProject(DTE2 dteObject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (dteObject == null)
                return null;
            Array prjs = null;
            try
            {
                prjs = (Array) dteObject.ActiveSolutionProjects;
            }
            catch
            {
                // When VS2010 is started from the command line,
                // we may catch a "Unspecified error" here.
            }
            if (prjs == null || prjs.Length < 1)
                return null;

            // don't handle multiple selection... use the first one
            if (prjs.GetValue(0) is Project)
                return prjs.GetValue(0) as Project;
            return null;
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Project pro = GetSelectedProject(LibManagerPackage.Instance.IDE);

            LibraryManagerDialog dlg = new LibraryManagerDialog(new LibraryDefinitions(_options.LibraryPackFiles), pro.Object as VCProject);
            dlg.Owner = Application.Current.MainWindow;

            dlg.ShowDialog();
        }
    }
}
