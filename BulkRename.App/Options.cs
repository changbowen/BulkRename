using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using static BulkRename.App.Helpers;
using Tomlyn.Model;
using System.Reflection.Emit;

namespace BulkRename.App
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    sealed class ConfigAttribute : Attribute
    {
        public string Section { get; set; }
        public string Key { get; set; }
    }


    /// <summary>
    /// TOML:
    ///   - Floats/Double are all mapped to C# double.
    ///   - Integers are all mapped to C# long.
    /// </summary>
    public class Options
    {
        public HashSet<string> ExplicitOpts = new();


        [Value(0, Required = true)]
        public IEnumerable<string> Paths { get; set; }


        private bool enumerate;
        [Config(Key = "enumerate", Section = "input")]
        [Option('e', "enumerate", HelpText = "Enumerate and include directory contents. Otherwise only specified paths are processed. Default is false.")]
        public bool Enumerate { get => enumerate; set { ExplicitOpts.Add(nameof(Enumerate)); enumerate = value; } }


        private bool recursive;
        [Config(Key = "recursive", Section = "input")]
        [Option('r', "recursive", HelpText = "Enumerate recursively. Default is false.")]
        public bool Recursive { get => recursive; set { ExplicitOpts.Add(nameof(Recursive)); recursive = value; } }


        private string searchPattern = @"*";
        [Config(Key = "search_pattern", Section = "input")]
        [Option('s', "search-pattern", HelpText = "Search pattern when enumerating. * and ? are supported. Default is *.")]
        public string SearchPattern { get => searchPattern; set { ExplicitOpts.Add(nameof(SearchPattern)); searchPattern = value; } }


        private string editorCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"notepad.exe" : @"vi";
        [Config(Key = "editor_command", Section = "edit")]
        [Option('c', "editor-command", HelpText = "Editor command line for editing names list. The path to the list file will be passed as the first parameter. If omitted, on Windows notepad.exe will be used, otherwise vi will be used.")]
        public string EditorCommand { get => editorCommand; set { ExplicitOpts.Add(nameof(EditorCommand)); editorCommand = value; } }


        private string editorArgs = @"{0}";
        [Config(Key = "editor_args", Section = "edit")]
        [Option('a', "editor-args", HelpText = "Arguments template. \"{0}\" will be replaced with the list file path. The arguments will be passed as a whole to the editor command. Default is {0}.")]
        public string EditorArgs { get => editorArgs; set { ExplicitOpts.Add(nameof(EditorArgs)); editorArgs = value; } }


        private long indentSize = 4;
        [Config(Key = "indent_size", Section = "edit")]
        [Option('i', "indent-size", HelpText = "Number of spaces of the indentation on each child items. Default is 4.")]
        public long IndentSize { get => indentSize; set { ExplicitOpts.Add(nameof(IndentSize)); indentSize = value; } }


        private bool verbose;
        [Config(Key = "verbose", Section = "misc")]
        [Option('v', "verbose", HelpText = "Enable verbose output. Default is false.")]
        public bool Verbose { get => verbose; set { ExplicitOpts.Add(nameof(Verbose)); verbose = value; } }


        private string tempNameGen = @"AlphaNum8";
        [Config(Key = "temp_name_gen", Section = "misc")]
        [Option('t', "temp-name-gen", HelpText = "Temporary name generator to use. Can be \"GUID\", \"AlphaNum#\" where # is a number (e.g. AlphaNum16), or \"AlphaNumVariableLength\". Default is AlphaNum8.")]
        public string TempNameGen
        {
            get => tempNameGen;
            set {
                ExplicitOpts.Add(nameof(TempNameGen));
                tempNameGen = value;

                // set the generator
                if (tempNameGen.StartsWith(@"AlphaNum", StringComparison.OrdinalIgnoreCase)) {
                    var lengthStr = tempNameGen[8..];
                    if (lengthStr.Equals(@"VariableLength", StringComparison.OrdinalIgnoreCase))
                        TempNameGenerator = len => GetRandomString(StringGenerator.RandomVariableLength, len);
                    else if (int.TryParse(lengthStr, out var len))
                        TempNameGenerator = _ => GetRandomString(len > 64 ? 64 : len);
                }
                else TempNameGenerator = _ => GetRandomString(StringGenerator.Guid);
            }
        }

        public Func<int, string> TempNameGenerator = _ => GetRandomString(8);


        public void MergeToml(TomlTable table)
        {
            if (table == null || table.Keys.Count == 0) return;

            foreach (var prop in typeof(Options).GetProperties()) {
                if (ExplicitOpts.Contains(prop.Name)) return; // skip params explicitly set in cmdline

                var cfgAttr = prop.GetCustomAttribute<ConfigAttribute>();
                if (cfgAttr == null) continue; // skip non-option attributes

                var tgtTable = cfgAttr.Section != null ? table.Get<TomlTable>(cfgAttr.Section) : table;
                if (tgtTable == null) continue;

                if (tgtTable.TryGet(cfgAttr.Key, prop.PropertyType, out var val))
                    prop.SetValue(this, val);
            }
        }

        //public void MergeIni(IniData ini)
        //{
        //    if (ini == null) return;

        //    foreach (var prop in typeof(Options).GetProperties()) {
        //        if (ExplicitOpts.Contains(prop.Name)) return; // skip params explicitly set in cmdline

        //        var cfgAttr = prop.GetCustomAttribute<ConfigAttribute>();
        //        if (cfgAttr == null) continue; // skip non-config attributes

        //        var dataCol = cfgAttr.Section != null ? ini[cfgAttr.Section] : ini.Global;
        //        if (dataCol == null || dataCol.Count == 0) continue;

        //        if (dataCol[cfgAttr.Key] is string val)
        //            prop.SetValue(this, val);
        //    }
        //}
    }
}
