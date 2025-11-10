using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tomlyn.Model;

namespace BulkRename.App
{
    public enum ConsoleMessageLevel
    {
        None, Info, Success,
        Warning, Error, Verbose
    }

    public enum PathType
    {
        None, File, Directory
    }

    public static class Helpers
    {
        internal static Random Random = new();
        internal static JsonSerializerOptions DefaultJsonSerializerOptions;

        static Helpers()
        {
            DefaultJsonSerializerOptions = new() { WriteIndented = true };
            DefaultJsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public static PathType GetPathType(string path)
        {
            if (Directory.Exists(path)) return PathType.Directory;
            else if (File.Exists(path)) return PathType.File;
            else return PathType.None;
        }

        /// <summary>
        /// Return <see cref="DirectoryInfo"/> or <see cref="FileInfo"/> depending on whether the path is a file or directory.
        /// Or null if the path is invalid or does not exist.
        /// </summary>
        public static FileSystemInfo GetFileSystemInfo(string path)
        {
            if (Directory.Exists(path)) return new DirectoryInfo(path);
            else if (File.Exists(path)) return new FileInfo(path);
            else return null;
        }

        public static void Move(this FileSystemInfo fsi, string destPath)
        {
            switch (fsi) {
                case FileInfo fi:
                    fi.MoveTo(destPath);
                    break;
                case DirectoryInfo di:
                    di.MoveTo(destPath);
                    break;
            }
        }

        public enum StringGenerator
        {
            /// <summary>
            /// A GUID string formatted with N. E.g. c5e3996e16fe494197633cdb386e7879.
            /// </summary>
            Guid,

            /// <summary>
            /// A random string representing a 32-bit integer padded with leading zeros.
            /// </summary>
            RandomInt,

            /// <summary>
            /// A random string containing digits and upper case letters in a specified length.
            /// </summary>
            RandomVariableLength
        }

        /// <summary>
        /// Get a pseudo-random string using a specific generator.
        /// </summary>
        /// <param name="length">Length is only used when <paramref name="generator"/> is set to <seealso cref="StringGenerator.RandomVariableLength"/>.</param>
        public static string GetRandomString(StringGenerator generator = StringGenerator.Guid, int length = default)
        {
            switch (generator) {
                case StringGenerator.Guid:
                default:
                    return Guid.NewGuid().ToString(@"N");
                case StringGenerator.RandomInt:
                    return Random.Next(0, int.MaxValue).ToString("D10");
                case StringGenerator.RandomVariableLength:
                    return GetRandomString(length);
            }
        }

        public static string GetRandomString(int length)
        {
            var chars = @"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var sb = new StringBuilder();
            for (int i = 0; i < length; i++) {
                sb.Append(chars[Random.Next(0, chars.Length)]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Check if <paramref name="subPath"/> is under <paramref name="parentPath"/>.
        /// If either parameter is null, returns false.
        /// </summary>
        public static bool IsDescendant(this string subPath, string parentPath)
        {
            if (subPath == null || parentPath == null) return false;
            return subPath.Length > parentPath.Length &&
                subPath.StartsWith(parentPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
        }

        public static void ConsoleWrite(Func<string> messageCallback, ConsoleMessageLevel type, bool newLine = true)
        {
            if (type == ConsoleMessageLevel.Verbose && !Program.Opts.Verbose) return;
            ConsoleWrite(messageCallback(), ConsoleMessageLevel.Verbose, newLine);
        }

        public static void ConsoleWrite(string message, ConsoleMessageLevel type, bool newLine = true)
        {
            ConsoleColor? fg = null;
            switch (type) {
                case ConsoleMessageLevel.Info: fg = ConsoleColor.Cyan; break;
                case ConsoleMessageLevel.Success: fg = ConsoleColor.Green; break;
                case ConsoleMessageLevel.Warning: fg = ConsoleColor.Yellow; break;
                case ConsoleMessageLevel.Error: fg = ConsoleColor.Red; break;
                case ConsoleMessageLevel.Verbose:
                    if (!Program.Opts.Verbose) return;
                    fg = ConsoleColor.DarkGray;
                    break;
            }
            ConsoleWrite(type > ConsoleMessageLevel.Success ? $@"[{type.ToString().ToUpper()}] {message}" : message, fg, null, newLine);
        }

        public static void ConsoleWrite(string message, ConsoleColor? fg = null, ConsoleColor? bg = null, bool newLine = true)
        {
            if (fg != null) Console.ForegroundColor = fg.Value;
            if (bg != null) Console.BackgroundColor = bg.Value;
            Console.Write(message + (newLine ? Environment.NewLine : string.Empty));
            Console.ResetColor();
        }

        public static string ConsolePrompt(string message, ConsoleColor? fg = null, ConsoleColor? bg = null, bool multiLine = false)
        {
            if (fg != null) Console.ForegroundColor = fg.Value;
            if (bg != null) Console.BackgroundColor = bg.Value;
            Console.Write(message);
            var input = Console.ReadLine() ?? string.Empty;
            if (multiLine) {
                while (Console.ReadLine() is string line && line.Trim().Length > 0) {
                    input += Environment.NewLine + line;
                }
            }
            Console.ResetColor();
            return input;
        }

        /// <summary>
        /// Generics version of <seealso cref="Get(TomlTable, string, Type)"/>.
        /// </summary>
        public static T Get<T>(this TomlTable tt, string key)
        {
            if (tt.ContainsKey(key) && tt[key] is T val) return val;
            return default;
        }

        /// <summary>
        /// Missing-key-safe and type-safe method for getting values from <see cref="TomlTable"/>.
        /// </summary>
        /// <typeparam name="T">Target type of value.</typeparam>
        /// <returns>Value object of the specified type if key exists and value is <paramref name="type"/>.
        /// Otherwise null.</returns>
        public static object Get(this TomlTable tt, string key, Type type)
        {
            if (tt.ContainsKey(key) && tt[key].GetType().IsAssignableFrom(type)) return tt[key];
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        public static bool TryGet(this TomlTable tt, string key, Type type, out object value)
        {
            if (tt.ContainsKey(key) && tt[key].GetType().IsAssignableFrom(type)) {
                value = tt[key];
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Missing-key-safe method for getting values from <see cref="TomlTable"/>.
        /// </summary>
        /// <returns>Value object if key exists. Otherwise null.</returns>
        public static object Get(this TomlTable tt, string key)
        {
            if (tt.ContainsKey(key)) return tt[key];
            return null;
        }

        public static bool TryGet<T>(this TomlTable tt, string key, out T value)
        {
            if (tt.ContainsKey(key) && tt[key] is T val) {
                value = val;
                return true;
            }
            value = default;
            return false;
        }


        public static string ToJson(this object obj) =>
            JsonSerializer.Serialize(obj, DefaultJsonSerializerOptions);

        /// <summary>
        /// Remove lines that are commented out from a string.
        /// </summary>
        /// <param name="commentChar">The comment symbol. Line content after it will be removed.</param>
        /// <param name="lineSelector">Optional selector to further transform line text after comments are removed.</param>
        /// <returns>All non-empty lines after all the transformations.</returns>
        public static IEnumerable<string> GetNonComments(this string text, char commentChar = '#', Func<string, string> lineSelector = null) => text
            .Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(s => {
                var i = s.IndexOf(commentChar, Program.PathComparison);
                var line = i > -1 ? s.Remove(i).Trim() : s;
                return lineSelector != null ? lineSelector(line) : line;
            })
            .Where(s => s.Length > 0);
    }
}
