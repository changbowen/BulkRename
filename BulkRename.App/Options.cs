using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using static BulkRename.App.Helpers;
using Tomlyn.Model;
using System.Runtime.Serialization;

namespace BulkRename.App
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    sealed class ConfigAttribute : Attribute
    {
        public string Section { get; set; }
        public string Key { get; set; }
    }


    public abstract class CommonOptions
    {
        [IgnoreDataMember] // excludes from toml serialization
        public HashSet<string> ExplicitOpts { get; } = new();


        private string editorCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"notepad.exe" : @"vi";
        [Config(Key = "editor_command", Section = "edit")]
        [Option('c', "editor-command", HelpText = "Editor command line for editing text content (e.g. names list, configuration etc.). If omitted, on Windows notepad.exe will be used, otherwise vi will be used.")]
        public string EditorCommand { get => editorCommand; set { ExplicitOpts.Add(nameof(EditorCommand)); editorCommand = value; } }


        private string editorArgs = @"{0}";
        [Config(Key = "editor_args", Section = "edit")]
        [Option('a', "editor-args", HelpText = "Arguments template. \"{0}\" will be replaced with the text file path. The arguments will be passed as a whole to the editor command. Default is {0}.")]
        public string EditorArgs { get => editorArgs; set { ExplicitOpts.Add(nameof(EditorArgs)); editorArgs = value; } }


        private bool verbose;
        [Config(Key = "verbose", Section = "misc")]
        [Option('v', "verbose", HelpText = "Enable verbose output. Default is false.")]
        public bool Verbose { get => verbose; set { ExplicitOpts.Add(nameof(Verbose)); verbose = value; } }


        public void MergeToml(TomlTable table)
        {
            if (table == null || table.Keys.Count == 0) return;

            foreach (var prop in this.GetType().GetProperties()) {
                if (ExplicitOpts.Contains(prop.Name)) continue; // skip params explicitly set in cmdline

                var cfgAttr = prop.GetCustomAttribute<ConfigAttribute>();
                if (cfgAttr == null) continue; // skip non-option attributes

                // look for config under seciton if set, or in global
                var tgtTable = cfgAttr.Section != null ? table.Get<TomlTable>(cfgAttr.Section) : table;
                if (tgtTable != null && tgtTable.TryGet(cfgAttr.Key, prop.PropertyType, out var val))
                    prop.SetValue(this, val);
            }
        }
    }


    /// <summary>
    /// Properties specific to config action.
    /// </summary>
    [Verb("config")]
    public class ConfigOptions : CommonOptions
    {
        private bool edit;
        [IgnoreDataMember] // excludes from toml serialization
        [Option('e', "edit", SetName = "config", HelpText = "Edit application configurations using the default editor.")]
        public bool Edit { get => edit; set { ExplicitOpts.Add(nameof(Edit)); edit = value; } }


        private bool menu;
        [IgnoreDataMember] // excludes from toml serialization
        [Option('m', "menu", SetName = "config", HelpText = "(Windows only) Add or remove the option to call the program in the shell context menu.")]
        public bool Menu { get => menu; set { ExplicitOpts.Add(nameof(Menu)); menu = value; } }
    }


    /// <summary>
    /// Properties specific to rename action.
    /// </summary>
    [Verb("rename", true)]
    public class RenameOptions : CommonOptions
    {
        [IgnoreDataMember] // excludes from toml serialization
        [Value(0, MetaName = "paths", Required = true, HelpText = "List of paths to process separated by spaces.")]
        public IEnumerable<string> Paths { get; set; }


        private bool enumerate;
        [Config(Key = "enumerate", Section = "input")]
        [Option('n', "enumerate", HelpText = "Enumerate and include directory contents. Otherwise only specified paths are processed. Default is false.")]
        public bool Enumerate { get => enumerate; set { ExplicitOpts.Add(nameof(Enumerate)); enumerate = value; } }


        private bool recursive;
        [Config(Key = "recursive", Section = "input")]
        [Option('r', "recursive", HelpText = "Enumerate recursively. Default is false.")]
        public bool Recursive { get => recursive; set { ExplicitOpts.Add(nameof(Recursive)); recursive = value; } }


        private string searchPattern = @"*";
        [Config(Key = "search_pattern", Section = "input")]
        [Option('s', "search-pattern", HelpText = "Search pattern when enumerating. * and ? are supported. Default is *.")]
        public string SearchPattern { get => searchPattern; set { ExplicitOpts.Add(nameof(SearchPattern)); searchPattern = value; } }


        private long indentSize = 4;
        [Config(Key = "indent_size", Section = "edit")]
        [Option('i', "indent-size", HelpText = "Number of spaces of the indentation on each child items. Default is 4.")]
        public long IndentSize { get => indentSize; set { ExplicitOpts.Add(nameof(IndentSize)); indentSize = value; } }


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


        private string commentSymbol = @"#";
        [Config(Key = "comment_symbol", Section = "misc")]
        [Option('m', "comment-symbol", HelpText = "The symbol for marking comments in the path list. This is useful when the default symbol exists in the paths. Default is #.")]
        public string CommentSymbol { get => commentSymbol; set { ExplicitOpts.Add(nameof(CommentSymbol)); commentSymbol = value; } }
    }
}
