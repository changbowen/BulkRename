using System;
using System.Text;
using Tomlyn;
using Tomlyn.Model;
using NaturalSort.Extension;
using System.Reflection;
using static BulkRename.App.Helpers;

namespace BulkRename.App
{
    public class Program
    {
        public static readonly string ProductName = nameof(BulkRename);
        public static readonly string ExePath = Environment.ProcessPath;
        public static readonly string ExeDir = Path.GetDirectoryName(ExePath);
        public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version;
        public static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
        public static readonly StringComparison PathComparison = StringComparison.Ordinal;

        public static Options Params { get; private set; }

        public static async Task Main(string[] args)
        {
            // get configs
            TomlTable toml = null;
            foreach (var cfgPath in new[] {
                @"BulkRename.App.cfg",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @$".{nameof(BulkRename).ToLowerInvariant()}")
            }) {
                if (!File.Exists(cfgPath)) continue;

                try { toml = Toml.ToModel(await File.ReadAllTextAsync(cfgPath, Encoding.UTF8)); }
                catch (Exception ex) {
                    ConsoleWrite($"Error when loading configuration file at {cfgPath}.\n{ex.Message}", ConsoleMessageLevel.Warning);
                };
                break;
            }

            // get command line args
            var opts = CommandLine.Parser.Default.ParseArguments<Options>(args).Value;
            if (opts == null) {
                ConsoleWrite($"Invalid arguments.", ConsoleMessageLevel.Error);
                return;
            }

            //// merge while giving cmd args priority
            if (toml != null) opts.MergeToml(toml);

            // start main program
            Params = opts;
            ConsoleWrite(() => $"Options: {Params.ToJson()}", ConsoleMessageLevel.Verbose);
            ConsoleWrite($"Starting program...", ConsoleMessageLevel.Verbose);
            await Run();
        }

        public static async Task Run()
        {
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
                        var lvl = child.FullName.Remove(0, parentPathWithSep.Length).Split(Path.DirectorySeparatorChar).Length;
                        if (lvl > child.Level) child.Level = lvl;
                        parent.Descendants.Add(child);
                    }
                    //if (child.ParentPath.Equals(parent.FullName)) parent.Children.Add(child);
                }
            }


            ConsoleWrite(() => Environment.NewLine + string.Join(Environment.NewLine, items.Select(item => $"{item.FullName} [{item.Type}]{(item.Type == PathType.Directory ? $" [{item.Descendants.Count} sub path(s)]" : null)}")), ConsoleMessageLevel.Verbose);

            var editorContent = @$"#############################################
#         Bulk Rename by Carl Chang         #
#############################################

# To rename:
#    1. Update each line with the new name.
#    2. Save and close the editor app.
#
# Unchanged lines will be skipped.
# Lines commented out are invalid paths that will be ignored.
# Adding or removing uncommented lines below will result in failure.
# New names cannot contain illegal characters: "" < > | : * ? \ /


{
                // ignore invalid ones
                string.Join(Environment.NewLine, items.Select(i =>
                    i.Skip ? @$"# {i.ErrMsg ?? @"[ERROR]"}" : $@"{(i.Level > 0 ? new string(' ', i.Level * (int)Params.IndentSize) : null)}{i.Name}"
                ))
}
";

            var toDo = items.Where(i => !i.Skip).ToArray();
            items = null;

            // create temp file
            var tmpConPath = Path.Combine(Path.GetTempPath(), GetRandomString(StringGenerator.Guid));
            try {
                // set temp content
                await File.WriteAllTextAsync(tmpConPath, editorContent, Encoding.UTF8);

                // open set editor
                await System.Diagnostics.Process
                    .Start(Params.EditorCommand, string.Format(Params.EditorArgs, tmpConPath))
                    .WaitForExitAsync();

                // parse modified content
                var newContent = await File.ReadAllTextAsync(tmpConPath, Encoding.UTF8);
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
                        var tmpName = Params.TempNameGenerator(item.Name.Length);
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