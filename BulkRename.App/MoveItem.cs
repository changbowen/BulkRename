namespace BulkRename.App
{

    public class MoveItem
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

        public List<MoveItem> Children { get; set; }

        /// <summary>
        /// How many levels of parent directories the path has. Zero means root level.
        /// Note the root is relative to the rest of the paths in a collection. Not to the file system.
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
        /// Initializes a <see cref="MoveItem"/> from the <paramref name="path"/>.
        /// Sets <see cref="ErrMsg"/> as needed.
        /// </summary>
        /// <param name="path">The path to the file or directory to be renamed.</param>
        /// <param name="newName">The new name of the item.</param>
        public MoveItem(string path, string newName = null)
        {
            path = path.TrimEnd(Path.DirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Invalid path.");

            FullName = Path.GetFullPath(path);
            Type = Helpers.GetPathType(path);
            if (Type == PathType.None) ErrMsg = @$"[INVALID] {path}";

            Name = Path.GetFileName(FullName);
            ParentPath = Path.GetDirectoryName(FullName);

            NewName = newName;
        }

        public MoveItem() { }

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
        public bool Move(string newPath, bool updateRef = true)
        {
            var dst = newPath?.TrimEnd(Path.DirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(dst)) return false;

            if (Type == PathType.File)
                File.Move(FullName, dst);
            else if (Type == PathType.Directory)
                Directory.Move(FullName, dst);
            else
                return false;

            FullName = dst;
            Name = Path.GetFileName(dst);
            ParentPath = Path.GetDirectoryName(dst);
            if (Children?.Count > 0) UpdateChildren();

            return true;
        }

        private void UpdateChildren()
        {
            foreach (var child in Children) {
                child.ParentPath = FullName;
                child.FullName = Path.Combine(FullName, child.Name);
                if (child.Children?.Count > 0) child.UpdateChildren();
            }
        }

        public static IEnumerable<MoveItem> EnumeratePath(string dirPath, string pattern, bool recursive) =>
            Directory.EnumerateFileSystemEntries(dirPath, pattern, SearchOption.TopDirectoryOnly)
            .SelectMany(s => {
                var item = new[] { new MoveItem(s) };
                if (recursive && item[0].Type == PathType.Directory) {
                    var children = EnumeratePath(s, pattern, recursive).ToList();
                    item[0].Children = children;
                    return item.Concat(children);
                }
                return item;
            });

    }
}
