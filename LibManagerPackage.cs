using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Controls;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;
using EnvDTE80;
using Microsoft;

namespace LibManager
{

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(LibManagerPackage.PackageGuidString)]
    [ProvideOptionPage(typeof(LibManagerOptions), "Library Manager", "General", 0, 0, true)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class LibManagerPackage : AsyncPackage
    {
        /// <summary>
        /// LibManagerPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "d6df78d5-20bb-4745-969c-4148a88854c3";

        /// <summary>
        /// Initializes a new instance of the <see cref="Command1Package"/> class.
        /// </summary>
        public LibManagerPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        static EventWaitHandle initDone = new EventWaitHandle(false, EventResetMode.ManualReset);
        static LibManagerPackage instance = null;

        /// <summary>
        /// Gets the instance of the package.
        /// </summary>
        public static LibManagerPackage Instance
        {
            get
            {
                initDone.WaitOne();
                return instance;
            }
        }

        #region Package Members

        //internal OutputWindowPane OutputWindow { get; private set; }
        internal LibManagerOptions Options { get; private set; }
        internal DTE2 IDE { get; private set; }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            instance = this;

            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            IDE = await GetServiceAsync(typeof(DTE)) as DTE2;
            Assumes.Present(IDE);
            Options = GetDialogPage(typeof(LibManagerOptions)) as LibManagerOptions;

            await ProjectContextMenu.InitializeAsync(this);

            //IDE = Package.GetGlobalService(typeof(DTE)) as DTE;
            //if (Dte == null)
            //    throw new Exception("Unable to get service: DTE");

            //var wnd = (OutputWindow) IDE.Windows.Item(EnvDTE.Constants.vsWindowKindOutput).Object;
            //if (OutputWindow == null)
            //    OutputWindow = wnd.OutputWindowPanes.Add("Lib Manager");

            //OutputWindow.OutputString("Lib Manager initialized\r\n");

            initDone.Set();
        }

        #endregion
    }
}
