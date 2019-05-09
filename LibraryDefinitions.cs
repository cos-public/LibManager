using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LibManager
{
    public class LibraryDefinitions
    {
        public struct LibraryConfig
        {
            public string IncludePath;
            public List<string> LibPaths;
            public List<string> LibFiles;
        }

        public struct LibraryEntry
        {
            public string Name;
            public string PackFile;
            public Dictionary<string, LibraryConfig> Conditions; //"Configuration|Platform", {includes, libs}
        }

        public List<LibraryEntry> Definitions = new List<LibraryEntry>();

        public LibraryDefinitions(List<string> files)
        {
            LoadLibraryDefinitions(files);
        }

        private void LoadLibraryDefinitions(List<string> files)
        {
            Definitions.Clear();

            Debug.Print("Loading library definitions");

            foreach (string file in files)
            {
                Debug.Print(String.Format("Parsing library pack file: {0}\r\n", file));
                var doc = new XmlDocument();
                doc.Load(file);
                foreach (XmlElement lib in doc.DocumentElement.ChildNodes)
                {
                    // each library in library pack file
                    var name = lib.SelectSingleNode("Name")?.InnerText;
                    Debug.Print(String.Format("Name: {0}", name));
                    var libPaths = new List<string>();
                    var libFiles = new List<string>();

                    foreach (XmlNode n in lib.SelectNodes("./LibPath"))
                    {
                        libPaths.Add(n.InnerText);
                    }

                    foreach (XmlNode n in lib.SelectNodes("./Lib"))
                    {
                        libFiles.Add(n.InnerText);
                    }
                    //can have only one IncludePath and only at top level (no per configuration IncludePath)
                    var includePath = lib.SelectSingleNode("./IncludePath")?.InnerText;

                    var conditions = new Dictionary<string, LibraryConfig>();

                    foreach (XmlElement c in lib.SelectNodes("Condition"))
                    {
                        var cfg = c.GetAttribute("Configuration");
                        var platform = c.GetAttribute("Platform");
                        var cfg_plat = cfg + "|" + platform;
                        Debug.Print(String.Format("Condition: {0}|{1}", cfg, platform));

                        var libPathsConditional = libPaths.GetRange(0, libPaths.Count); //clone
                        foreach (XmlNode n in c.SelectNodes("./LibPath"))
                        {
                            libPathsConditional.Add(n.InnerText);
                        }

                        var libFilesConditional = libFiles.GetRange(0, libFiles.Count); //clone
                        foreach (XmlNode n in c.SelectNodes("./Lib"))
                        {
                            libFilesConditional.Add(n.InnerText);
                        }

                        Debug.Print(String.Format("IncludePath: {0}; LibPath: {1}; Lib: {2}",
                            includePath, String.Join(";", libPathsConditional.ToArray()), String.Join(";", libFilesConditional.ToArray())));
                        conditions.Add(cfg_plat, new LibraryConfig() { IncludePath = includePath, LibPaths = libPathsConditional, LibFiles = libFilesConditional });
                    }

                    Definitions.Add(new LibraryEntry() { Name = name, PackFile = file, Conditions = conditions });
                }
            }
        }
    }
}
