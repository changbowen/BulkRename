# BulkRename

Rename files by editing the file tree in a text editor.

```powershell
.\BulkRename.App.exe
  -n, --enumerate         Enumerate and include directory contents. Otherwise only specified paths are processed.
                          Default is false.
  -r, --recursive         Enumerate recursively. Default is false.
  -s, --search-pattern    Search pattern when enumerating. * and ? are supported. Default is *.
  -i, --indent-size       Number of spaces of the indentation on each child items. Default is 4.
  -t, --temp-name-gen     Temporary name generator to use. Can be "GUID", "AlphaNum#" where # is a number (e.g.
                          AlphaNum16), or "AlphaNumVariableLength". Default is AlphaNum8.
  -c, --editor-command    Editor command line for editing text content (e.g. names list, configuration etc.). If
                          omitted, on Windows notepad.exe will be used, otherwise vi will be used.
  -a, --editor-args       Arguments template. "{0}" will be replaced with the text file path. The arguments will be
                          passed as a whole to the editor command. Default is {0}.
  -v, --verbose           Enable verbose output. Default is false.
  --help                  Display this help screen.
  --version               Display version information.
  paths (pos. 0)          Required. List of paths to process separated by spaces.
```

```powershell
.\BulkRename.App.exe config --help
  -e, --edit              Edit application configurations using the default editor.
  -m, --menu              (Windows only) Add or remove the option to call the program in the shell context menu.
  -c, --editor-command    Editor command line for editing text content (e.g. names list, configuration etc.). If
                          omitted, on Windows notepad.exe will be used, otherwise vi will be used.
  -a, --editor-args       Arguments template. "{0}" will be replaced with the text file path. The arguments will be
                          passed as a whole to the editor command. Default is {0}.
  -v, --verbose           Enable verbose output. Default is false.
  --help                  Display this help screen.
  --version               Display version information.
```