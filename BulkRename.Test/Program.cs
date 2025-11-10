
var dir = "testfiles";
if (Directory.Exists(dir)) Directory.Delete(dir, true);
Directory.CreateDirectory(dir);

createTest2();

await BulkRename.App.Program.Main([
    "-v",
    "--enumerate",
    "--recursive",
    dir,
]);

return;

# region Test1
void createTest1()
{
    File.WriteAllText(Path.Combine(dir, "1.txt"), "1");
    File.WriteAllText(Path.Combine(dir, "2.txt"), "2");
    File.WriteAllText(Path.Combine(dir, "3"), "3");
    File.WriteAllText(Path.Combine(dir, "4.txt"), "4");
    File.WriteAllText(Path.Combine(dir, "10.txt"), "10");
    File.WriteAllText(Path.Combine(dir, "5"), "5");

}
#endregion

#region Test2
void createTest2()
{
    File.WriteAllText($"{dir}\\file1.txt", "file 1");
    File.WriteAllText($"{dir}\\file2.txt", "file 2");
    Directory.CreateDirectory($"{dir}\\dir1");
    Directory.CreateDirectory($"{dir}\\dir2");
    File.WriteAllText($"{dir}\\dir2\\file3.txt", "file 3");
    File.WriteAllText($"{dir}\\dir2\\file4.txt", "file 4");
    Directory.CreateDirectory($"{dir}\\dir2\\subdir1");
    File.WriteAllText($"{dir}\\dir2\\subdir1\\file5.txt", "file 5");
    File.WriteAllText($"{dir}\\dir2\\subdir1\\file6.txt", "file 6");
    Directory.CreateDirectory($"{dir}\\dir2\\subdir1\\subsubdir1");
    File.WriteAllText($"{dir}\\dir2\\subdir1\\subsubdir1\\file7.txt",  "file 7");
}
#endregion

