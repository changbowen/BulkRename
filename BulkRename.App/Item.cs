using static BulkRename.App.Helpers;

namespace BulkRename.App
{

    public class Item
    {
        /// <summary>
        /// Full path of the item.
        /// Directory separation character will always be trimmed.
        /// </summary>
        public string FullName { get; private set; }

        public PathType Type { get; private set; }

        public string Name { get; private set; }

        /// <summary>
        /// Parent full path of the item.
        /// Returns null when item is a root object.
        /// </summary>
        public string ParentPath { get; private set; }

        /// <summary>
        /// All sub items of the item.
        /// Only created when the item is a directory.
        /// </summary>
        public List<Item> Descendants { get; private set; }

        /// <summary>
        /// How many levels of parent directories the path has. Zero means root level.
        /// The level only reflects the items in the list.
        /// E.g. with only <c>\a</c> and <c>\a\b\c</c> in the list, the level of <c>\a</c> is 0 and <c>\a\b\c</c> is 1 instead of 2.
        /// The root is relative to the rest of the paths in a collection. Not to the file system.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// The new file name to change to.
        /// </summary>
        public string NewName { get; set; }

        /// <summary>
        /// Error information of the path. Can be used for filtering invalid paths.
        /// </summary>
        public string ErrMsg { get; set; }

        public bool Skip => Type == PathType.None || ErrMsg != null;

        /// <summary>
        /// Initializes a <see cref="Item"/> from the <paramref name="path"/>.
        /// Sets <see cref="ErrMsg"/> as needed.
        /// </summary>
        /// <param name="path">The path to the file or directory to be renamed.</param>
        /// <param name="newName">The new name of the item.</param>
        public Item(string path, string newName = null)
        {
            path = path.TrimEnd(Path.DirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Invalid path.");

            FullName = Path.GetFullPath(path);
            Type = GetPathType(path);
            if (Type == PathType.None) ErrMsg = @$"[INVALID] {path}";
            if (Type == PathType.Directory) Descendants = new();

            Name = Path.GetFileName(FullName);
            ParentPath = Path.GetDirectoryName(FullName);

            NewName = newName;
        }

        public Item() { }

        //public bool Rename(string newName = null)
        //{
        //    var name = newName ?? NewName;
        //    if (string.IsNullOrWhiteSpace(name)) return false;
        //    var newPath = Path.Combine(ParentPath, name);
        //    if (Move(newPath, false)) {
        //        FullName = newPath;
        //        Name = Path.GetFileName(name);
        //        return true;
        //    }
        //    return false;
        //}

        /// <summary>
        /// Move item to <paramref name="destPath"/> if set, or to <see cref="NewName"/>.
        /// Relative properties will point to the new item.
        /// </summary>
        /// <param name="destPath">Full path of the destination including the name.</param>
        public bool Move(string newPath)
        {
            if (Type != PathType.File && Type != PathType.Directory) return false;
            
            var dst = newPath?.TrimEnd(Path.DirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(dst)) return false;

            var src = FullName;
            ConsoleWrite($"{src} >>> {dst}...", ConsoleMessageLevel.Verbose);

            var parentDir = Path.GetDirectoryName(dst);
            //Directory.CreateDirectory(parentDir);

            if (Type == PathType.File)
                File.Move(src, dst);
            else if (Type == PathType.Directory)
                Directory.Move(src, dst);
            else
                return false;
            
            FullName = dst;
            Name = Path.GetFileName(dst);
            ParentPath = parentDir;
            if (Descendants?.Count > 0) {
                foreach (var desc in Descendants) {
                    desc.ParentPath = desc.ParentPath.Replace(src, dst, Program.PathComparison);
                    desc.FullName = Path.Combine(desc.ParentPath, desc.Name);
                }
            }

            return true;
        }

        //private static void UpdateChildren(Item parent)
        //{
        //    foreach (var child in parent.Children) {
        //        child.ParentPath = parent.FullName;
        //        child.FullName = Path.Combine(parent.FullName, child.Name);
        //        if (child.Children?.Count > 0) UpdateChildren(child);
        //    }
        //}

        //public static IEnumerable<Item> EnumeratePath(string dirPath, string pattern, bool recursive) =>
        //    Directory.EnumerateFileSystemEntries(dirPath, pattern, SearchOption.TopDirectoryOnly)
        //    .SelectMany(s => {
        //        var item = new[] { new Item(s) };
        //        if (recursive && item[0].Type == PathType.Directory) {
        //            var children = EnumeratePath(s, pattern, recursive).ToList();
        //            item[0].Children = children;
        //            return item.Concat(children);
        //        }
        //        return item;
        //    });

    }
}
