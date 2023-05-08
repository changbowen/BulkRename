using System;
using CommandLine;
using System.Diagnostics;
using static BulkRename.App.Helpers;
using NaturalSort.Extension;
using System.Text.Json;

namespace BulkRename.App
{
    public class Program
    {
        public class Options
        {
            [Value(0, Required = true)]
            public IEnumerable<string> Paths { get; set; }

            [Option('e', "enumerate", Required = false, Default = false, HelpText = "Enumerate and include directory contents. Otherwise only specified paths are processed.")]
            public bool Enumerate { get; set; }

            [Option('r', "recursive", Required = false, Default = false, HelpText = "Enumerate recursively.")]
            public bool Recursive { get; set; }

            [Option('s', "searchpattern", Default = @"*", HelpText = "Search pattern when enumerating. * and ? are supported.")]
            public string SearchPattern { get; set; }

            [Option('v', "verbose", Required = false, Default = false, HelpText = "Enable verbose output.")]
            public bool Verbose { get; set; }

            [Option('c', "editorcmd", Required = false, Default = @"notepad.exe", HelpText = "Editor command line for editing names list. The path to the list file will be passed as the first parameter.")]
            public string EditorCommand { get; set; }

            [Option('t', "tempnametype", Required = false, Default = StringGenerator.RandomVariableLength, HelpText = "Temporary name generator to use.")]
            public StringGenerator TempNameType { get; set; }

            [Option('i', "indentsize", Required = false, Default = 4, HelpText = "Number of spaces of the indentation on each child items.")]
            public int IndentSize { get; set; }
        }

        public static readonly string ProductName = nameof(BulkRename);
        public static readonly string ExePath = Environment.ProcessPath;
        public static readonly string ExeDir = Path.GetDirectoryName(ExePath);
        public static readonly Version Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        public static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
        public static readonly StringComparison PathComparison = StringComparison.Ordinal;

        public static Options Params { get; private set; }

        public static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(RunWithOptions);
        }

        public static async Task RunWithOptions(Options opt)
        {
            Params = opt;

            // convert to array of Item
            var items = Params.Paths.Where(path => path?.Trim().TrimEnd(Path.DirectorySeparatorChar).Length > 0)
                .Distinct()
                .SelectMany(path => {
                    // make sure all paths are valid and add to list
                    var item = new Item(path);
                    if (item.Type == PathType.None)
                        ConsoleWrite($"Path does not exist: {path}", ConsoleMessageLevel.Warning);
                    else if (item.ParentPath == null)
                        ConsoleWrite($"Unable to access parent location of path: {path}", ConsoleMessageLevel.Warning);
                    else if (item.Type == PathType.Directory && Params.Enumerate) {
                        return Directory.EnumerateFileSystemEntries(path, Params.SearchPattern,
                            Params.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Select(s => new Item(s))
                            .Concat(new[] { item });
                        //return Item.EnumeratePath(path, Params.SearchPattern, Params.Recursive).Concat(new[] { item });
                    }
                    return new[] { item };
                })
                .OrderBy(i => i.FullName, PathComparison.WithNaturalSort())
                .ToArray();

            if (items.Length == 0) {
                ConsoleWrite("Couldn't find valid files or folders to rename.", ConsoleMessageLevel.Warning);
                return;
            }

            // set item relationship
            foreach (var child in items) {
                if (child.Type == PathType.None) continue;
                foreach (var parent in items) {
                    if (parent.Type != PathType.Directory || child.FullName.Length <= parent.FullName.Length) continue;
                    var parentPathWithSep = parent.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    if (child.FullName.StartsWith(parentPathWithSep)) {
                        child.Level += child.FullName.Remove(0, parentPathWithSep.Length).Split(Path.DirectorySeparatorChar).Length;
                        parent.Descendants.Add(child);
                    }
                    //if (child.ParentPath.Equals(parent.FullName)) parent.Children.Add(child);
                }
            }

#if DEBUG
            ConsoleWrite(JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true }), ConsoleMessageLevel.Debug);
#endif

            var editorContent = @$"#############################################
#         BULK RENAME by Carl Chang         #
#############################################

# To rename:
#    1. Update each line with the new name.
#    2. Save and close the editor app.
#
# Unchanged lines will be skipped.
# Do NOT add or remove uncommented lines!
# Lines commented out are invalid paths that must be ignored.
# New name cannot contain illegal characters: "" < > | : * ? \ /


{
                // ignore invalid ones
                string.Join(Environment.NewLine, items.Select(i =>
                    i.Skip ? @$"# {i.ErrMsg ?? @"[ERROR]"}" : $@"{(i.Level > 0 ? new string(' ', i.Level * Params.IndentSize) : null)}{i.Name}"
                ))
}
";

            var toDo = items.Where(i => !i.Skip).ToArray();
            items = null;

            // create temp file
            var tmpConPath = Path.Combine(Path.GetTempPath(), GetRandomString());
            try {
                // set temp content
                await File.WriteAllTextAsync(tmpConPath, editorContent, System.Text.Encoding.UTF8);

                // open set editor
                await Process.Start(Params.EditorCommand, tmpConPath).WaitForExitAsync();

                // parse modified content
                var newContent = await File.ReadAllTextAsync(tmpConPath, System.Text.Encoding.UTF8);
                var newNames = newContent.Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => {
                        var i = s.IndexOf('#', PathComparison);
                        return i > -1 ? s.Remove(i).Trim() : s;
                    }).Where(s => s.Length > 0).ToArray();
                
                // sanity check
                if (newNames.Length == 0) {
                    ConsoleWrite($"Empty list file.", ConsoleMessageLevel.Warning);
                    return;
                }
                else if (newNames.Length != toDo.Length) {
                    ConsoleWrite($"Mismatch number of lines.", ConsoleMessageLevel.Error);
                    return;
                }

                // more sanity checks
                if (newNames.Any(s => InvalidFileNameChars.Any(c => s.Contains(c, PathComparison)))) {
                    ConsoleWrite($"Invalid characters in new name.", ConsoleMessageLevel.Error);
                    return;
                }

                // fill new names
                for (int i = 0; i < toDo.Length; i++) {
                    toDo[i].NewName = newNames[i];
                }

                // more sanity checks
                if (toDo.Length != toDo.Select(s => Path.Combine(s.ParentPath, s.NewName)).Distinct().Count()) {
                    ConsoleWrite($"Duplicate file names.", ConsoleMessageLevel.Error);
                    return;
                }

                toDo = toDo
                    .Where(i => !i.Name.Equals(i.NewName, PathComparison)) // skip unchanged ones while supporting changing letter case
                    .ToArray();

                // rename to temp names to support reusing file names
                foreach (var item in toDo) {
                    var loopCount = 0;
                    while (true)
                    {
                        if (loopCount > 10000) throw new Exception("Maximum temporary name limit reached.");
                        loopCount++;
                        var tmpName = GetRandomString(Params.TempNameType, item.Name.Length);
                        var tmpPath = Path.Combine(item.ParentPath, tmpName);

                        // get another tmpName when it already exists
                        if (GetPathType(tmpPath) != PathType.None) continue;

                        item.Move(tmpPath);
                        break;
                    }
                }

                // move back to target names
                foreach (var item in toDo) {
                    var newPath = Path.Combine(item.ParentPath, item.NewName);
                    item.Move(newPath);
                }
            }
            finally {
                File.Delete(tmpConPath);
            }


            ConsoleWrite("Operation completed.", ConsoleMessageLevel.Success);
            //Console.ReadLine();
        }
    }
}