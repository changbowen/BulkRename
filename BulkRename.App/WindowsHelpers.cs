using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace BulkRename.App
{
    [SupportedOSPlatform(@"windows")]
    internal class WindowsHelpers
    {
        #region Windows Explorer Context Menu
        internal static readonly string[] ClassSubDirs = new[] { @"*", @"Directory" };

        public static bool CheckExplorerMenuItem()
        {
            return ClassSubDirs.All(clsDir => {
                using (var itmKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{clsDir}\shell\{Program.ProductName}")) {
                    return itmKey != null;
                }
            });
        }

        public static void SetExplorerMenuItem()
        {
            // create submenu
            using (var itmKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{Program.ProductName}\shell\BulkRename")) {
                itmKey.SetValue(@"MUIVerb", "Bulk Rename", RegistryValueKind.String);
                using (var cmdKey = itmKey.CreateSubKey(@"command")) {
                    cmdKey.SetValue(null, $@"""{Program.ExePath}"" ""%1""", RegistryValueKind.String);
                }
            }

            foreach (var clsDir in ClassSubDirs) {
                // create ref to submenu
                using (var zivKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{clsDir}\shell\{Program.ProductName}")) {
                    zivKey.SetValue(@"Icon", $@"""{Program.ExePath}""", RegistryValueKind.String);
                    zivKey.SetValue(@"ExtendedSubCommandsKey", Program.ProductName, RegistryValueKind.String);
                }
            }
        }

        public static void ClearExplorerMenuItem()
        {
            // delete ref to submenu
            foreach (var clsDir in ClassSubDirs) {
                using (var dirKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{clsDir}", true)) {
                    if (dirKey == null) continue;
                    dirKey.DeleteSubKeyTree($@"shell\{Program.ProductName}", false);
                    //shell key might be created by this program. Delete it when nothing is underneath.
                    using (var shlKey = dirKey.OpenSubKey(@"shell", true)) {
                        if (shlKey != null && shlKey.SubKeyCount == 0 && shlKey.ValueCount == 0) dirKey.DeleteSubKeyTree("shell");
                    }
                }
            }

            // delete submenu
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{Program.ProductName}", false);
        }
        #endregion

    }
}
