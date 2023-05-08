using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BulkRename.App
{
    public enum ConsoleMessageLevel
    {
        None, Info, Success,
        Warning, Error, Verbose, Debug
    }

    public enum PathType
    {
        None, File, Directory
    }

    public static class Helpers
    {
        internal static Random Random = new();
        public enum StringGenerator { Guid, ShortGuid, RandomVariableLength }

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
                case StringGenerator.ShortGuid:
                    return Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");
                case StringGenerator.RandomVariableLength:
                    var chars = @"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    var sb = new StringBuilder();
                    for (int i = 0; i < length; i++) {
                        sb.Append(chars[Random.Next(0, chars.Length)]);
                    }
                    return sb.ToString();
            }
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

        public static void ConsoleWrite(string message, ConsoleMessageLevel type, bool newLine = true)
        {
            ConsoleColor? fg = null;
            switch (type) {
                case ConsoleMessageLevel.Info: fg = ConsoleColor.Cyan; break;
                case ConsoleMessageLevel.Success: fg = ConsoleColor.Green; break;
                case ConsoleMessageLevel.Warning: fg = ConsoleColor.Yellow; break;
                case ConsoleMessageLevel.Error: fg = ConsoleColor.Red; break;
                case ConsoleMessageLevel.Verbose:
                    if (!Program.Params.Verbose) return;
                    fg = ConsoleColor.DarkGray;
                    break;
                case ConsoleMessageLevel.Debug: fg = ConsoleColor.Magenta; break;
            }
            ConsoleWrite(type > ConsoleMessageLevel.Success ? $@"{type.ToString().ToUpper()}: {message}" : message, fg, null, newLine);
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

    }
}
