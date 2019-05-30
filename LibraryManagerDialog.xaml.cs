using Microsoft;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LibManager
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class LibraryManagerDialog : DialogWindow
    {
        private LibraryDefinitions _definitions;
        private VCProject _project;

        public class LibraryConfigState
        {
            public enum State { All, Partial, None, Empty }; //None - not configured, Empty - no configuration
            public State ContainsIncludePath { get; set; } = State.None;
            public State ContainsLibDir { get; set; } = State.None;
            public State ContainsLib { get; set; } = State.None;
            public override string ToString() {
                return String.Format("I:{0} L:{1} l:{2}", ContainsIncludePath, ContainsLibDir, ContainsLib);
            }
        }

        public class LibraryState
        {
            public string Library { get; set; }
            public LibraryDefinitions.LibraryEntry DefinitionsEntry;
            public Dictionary<string, LibraryConfigState> Configurations { get; set; }
        }

        public List<LibraryState> LibraryStates = new List<LibraryState>();

        private string GetAbsolutePath(string path, string relative_to)
        {
            if (!System.IO.Path.IsPathRooted(path))
            {
                // relative include path, consider it's relative to libpack_file
                var full_path = relative_to + "\\" + path;
                // remove redundant ..
                full_path = System.IO.Path.GetFullPath((new Uri(full_path)).LocalPath);
                return full_path;
            }
            return path;
        }

        private List<string> GetAbsolutePaths(List<string> paths, string relative_to)
        {
            var r = new List<string>();
            foreach (string p in paths)
            {
                r.Add(GetAbsolutePath(p, relative_to));
            }
            return r;
        }

        public LibraryManagerDialog(LibraryDefinitions definitions, VCProject project)
        {
            Assumes.NotNull(definitions);
            Assumes.NotNull(project);

            _definitions = definitions;
            _project = project;

            InitializeComponent();

            UpdateLibraryConfigState();

            foreach (VCConfiguration config in (IVCCollection)_project.Configurations)
            {
                GridViewColumn column = new GridViewColumn();
                column.Header = config.Name;

                DataTemplate template = new DataTemplate();
                FrameworkElementFactory factory = new FrameworkElementFactory(typeof(LibraryConfigStateIndicator));
                factory.SetValue(LibraryConfigStateIndicator.StateProperty, new Binding(string.Format("Configurations[{0}]", config.Name)));
                template.VisualTree = factory;
                column.CellTemplate = template;
                var grid = (LibraryListView.View as GridView);
                grid.Columns.Insert(grid.Columns.Count - 1, column);
            }

            LibraryListView.ItemsSource = LibraryStates;
        }

        private void UpdateLibraryConfigState()
        {
            LibraryStates.Clear();
            Debug.Print("Checking libraries");
            //check also
            //VCPlatform.IncludeDirectories
            //VCPlatform.LibraryDirectories (LIB env)

            //VCProjectEngine.Evaluate to expand macros

            var project_path = _project.ProjectDirectory;
            Assumes.True(System.IO.Path.IsPathRooted(project_path));
            Assumes.True(System.IO.Directory.Exists(project_path));

            foreach (var e in _definitions.Definitions)
            {
                Debug.Print("\n=== Checking library {0}", e.Name);
                var libpack_path = System.IO.Path.GetDirectoryName(e.PackFile);
                Assumes.True(System.IO.Path.IsPathRooted(libpack_path));
                Assumes.True(System.IO.Directory.Exists(libpack_path));

                var lib_config = new Dictionary<string, LibraryConfigState>();

                foreach (VCConfiguration config in (IVCCollection)_project.Configurations)
                {
                    Debug.Print("* for configuration {0}", config.Name);
                    LibraryDefinitions.LibraryConfig lib_conf;
                    if (!e.Conditions.TryGetValue(config.Name, out lib_conf))
                    {
                        Debug.Print("has no library configuration");
                        lib_config.Add(config.Name, null);
                        continue;
                    }
                    Debug.Print("has config: IncludePath={0}; LibPaths={1}; LibFiles={2}",
                        lib_conf.IncludePath, String.Join(";", lib_conf.LibPaths.ToArray()), String.Join(";", lib_conf.LibFiles.ToArray()));
                    LibraryConfigState lib_config_state = new LibraryConfigState();

                    var compiler = (VCCLCompilerTool)((IVCCollection)config.Tools).Item("VCCLCompilerTool");
                    var linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");

                    Debug.Print("project FullIncludePath({0}): {1}", config.Name, compiler.FullIncludePath);
                    lib_config_state.ContainsIncludePath = ModifyIncludePath(compiler, lib_conf.IncludePath, project_path, libpack_path, true);
                    Debug.Print("contains lib include: {0}", lib_config_state.ContainsIncludePath);

                    Debug.Print("project AdditionalLibraryDirectories({0}): {1}", config.Name, linker.AdditionalLibraryDirectories);
                    lib_config_state.ContainsLibDir = ModifyLibPath(linker, lib_conf.LibPaths, project_path, libpack_path, true);
                    Debug.Print("contains lib dir: {0}", lib_config_state.ContainsLibDir);

                    Debug.Print("project AdditionalDependencies({0}): {1}", config.Name, linker.AdditionalDependencies);
                    lib_config_state.ContainsLib = ModifyLibDependencies(linker, lib_conf.LibFiles, project_path, libpack_path, true);
                    Debug.Print("contains lib dependency: {0}", lib_config_state.ContainsLib);

                    lib_config.Add(config.Name, lib_config_state);
                }

                LibraryStates.Add(new LibraryState() { Library = e.Name, DefinitionsEntry = e, Configurations = lib_config });
            }

            //VCConfiguration c = _project.ActiveConfiguration;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void UseLibrary_Click(object sender, RoutedEventArgs e)
        {
            var lib_state = ((sender as System.Windows.Controls.Button).DataContext as LibManager.LibraryManagerDialog.LibraryState);
            Debug.Print("Use library {0}", lib_state.Library);

            LibraryDefinitions.LibraryEntry lib_def = lib_state.DefinitionsEntry;
            var project_path = _project.ProjectDirectory;
            var libpack_path = System.IO.Path.GetDirectoryName(lib_def.PackFile);

            foreach (VCConfiguration config in (IVCCollection)_project.Configurations)
            {
                Debug.Print("Update configuration {0}", config.Name);
                LibraryDefinitions.LibraryConfig lib_conf;
                if (!lib_def.Conditions.TryGetValue(config.Name, out lib_conf))
                {
                    Debug.Print("has no library configuration");
                    continue;
                }

                var compiler = (VCCLCompilerTool)((IVCCollection)config.Tools).Item("VCCLCompilerTool");
                var linker = (VCLinkerTool)((IVCCollection)config.Tools).Item("VCLinkerTool");

                Debug.Print("IncludePath: {0}, LibPaths: {1}, Libs: {2}",
                    lib_conf.IncludePath, String.Join(";", lib_conf.LibPaths.ToArray()), String.Join(";", lib_conf.LibFiles.ToArray()));

                ModifyIncludePath(compiler, lib_conf.IncludePath, project_path, libpack_path, false);
                ModifyLibPath(linker, lib_conf.LibPaths, project_path, libpack_path, false);
                ModifyLibDependencies(linker, lib_conf.LibFiles, project_path, libpack_path, false);
            }

            UpdateLibraryConfigState();
            LibraryListView.ItemsSource = null;
            LibraryListView.ItemsSource = LibraryStates;
        }

        private LibraryConfigState.State ModifyIncludePath(VCCLCompilerTool compiler, string include_path, string project_path, string libpack_path, bool check_only = false)
        {
            if (include_path == null)
                return LibraryConfigState.State.Empty;

            var full_include_path = GetAbsolutePath(include_path, libpack_path);
            var includes = new List<string>(SplitQuoted(";", compiler.AdditionalIncludeDirectories));
            var include_found = false;
            foreach (string project_include in includes)
            {
                string full_project_include = GetAbsolutePath(project_include, project_path);
                if (full_project_include.Equals(full_include_path, StringComparison.OrdinalIgnoreCase))
                {
                    include_found = true;
                    break;
                }
            }

            if (!check_only & !include_found)
            {
                includes.Add(full_include_path);
                compiler.AdditionalIncludeDirectories = String.Join(";", includes.ToArray());
            }

            return include_found ? LibraryConfigState.State.All : LibraryConfigState.State.None;
        }

        private LibraryConfigState.State ModifyLibPath(VCLinkerTool linker, List<string> lib_dirs, string project_path, string libpack_path, bool check_only = false)
        {
            if (lib_dirs.Count == 0)
                return LibraryConfigState.State.Empty;

            var full_lib_dirs = GetAbsolutePaths(lib_dirs, libpack_path);
            bool found = false;

            var project_lib_dirs = new List<string>(SplitQuoted(";", linker.AdditionalLibraryDirectories));
            foreach (var project_libdir in project_lib_dirs)
            {
                string full_project_libdir = GetAbsolutePath(project_libdir, project_path);
                found |= full_lib_dirs.RemoveAll(n => n.Equals(full_project_libdir, StringComparison.OrdinalIgnoreCase)) > 0;
            }

            if (!check_only)
            {
                project_lib_dirs.AddRange(full_lib_dirs);
                linker.AdditionalLibraryDirectories = String.Join(";", project_lib_dirs.ToArray());
            }

            return full_lib_dirs.Count == 0 ? LibraryConfigState.State.All
                : found ? LibraryConfigState.State.Partial : LibraryConfigState.State.None;
        }

        private LibraryConfigState.State ModifyLibDependencies(VCLinkerTool linker, List<string> lib_files, string project_path, string libpack_path, bool check_only = false)
        {
            if (lib_files.Count == 0)
                return LibraryConfigState.State.Empty;

            var lib_files_remaining = lib_files.GetRange(0, lib_files.Count);
            bool found = false;

            var project_libs = new List<string>(SplitQuoted(" ", linker.AdditionalDependencies));
            foreach (var project_lib in project_libs)
            {
                found |= lib_files_remaining.Remove(project_lib);
            }

            if (!check_only)
            {
                project_libs.AddRange(lib_files_remaining);
                linker.AdditionalDependencies = String.Join(" ", project_libs);
            }

            return lib_files_remaining.Count == 0 ? LibraryConfigState.State.All
                : found ? LibraryConfigState.State.Partial : LibraryConfigState.State.None;
        }

        private string[] SplitQuoted(string sep, string line)
        {
            List<string> list = new List<string>();
            StringBuilder word = new StringBuilder();
            int doubleQuoteCount = 0;
            for (int i = 0; i < line.Length; i++)
            {
                string chr = line[i].ToString();
                if (chr == "\"")
                {
                    if (doubleQuoteCount == 0)
                        doubleQuoteCount++;
                    else
                        doubleQuoteCount--;

                    continue;
                }
                if (chr == sep && doubleQuoteCount == 0)
                {
                    list.Add(word.ToString());
                    word = new StringBuilder();
                    continue;
                }
                word.Append(chr);
            }

            list.Add(word.ToString());

            return list.ToArray();
        }

        private void UnuseLibrary_Click(object sender, RoutedEventArgs e)
        {
            Debug.Print("Unuse library");
        }
    }
}
