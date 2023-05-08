
var dir = "testfiles";
if (Directory.Exists(dir)) Directory.Delete(dir, true);
Directory.CreateDirectory(dir);

createTest2();
await BulkRename.App.Program.Main(new[] {
    Path.Combine(dir, "1"),
    Path.Combine(dir, "2"),
    Path.Combine(dir, "2", "1.txt"),
    Path.Combine(dir, "2", "2.txt"),
    Path.Combine(dir, "2\\22\\222", "222.txt"),
    Path.Combine(dir, "10"),
    Path.Combine(dir, "3"),
    "-v",
});

//await BulkRename.App.Program.Main(new[] {
//    "-v",
//    "--enumerate",
//    "--recursive",
//    "testfiles",
//});

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

createTest1();
await BulkRename.App.Program.Main(new[] {
    Path.Combine(dir, "1.txt"),
    Path.Combine(dir, "2.txt"),
    Path.Combine(dir, "3"),
    Path.Combine(dir, "nonexist"),
    Path.Combine(dir, "10.txt"),
    Path.Combine(dir, "5"),
});
#endregion

#region Test2
void createTest2()
{
    Directory.CreateDirectory(Path.Combine(dir, "1"));
    Directory.CreateDirectory(Path.Combine(dir, "2"));
    File.WriteAllText(Path.Combine(dir, "2", "1.txt"),  "1");
    File.WriteAllText(Path.Combine(dir, "2", "2.txt"),  "2");
    Directory.CreateDirectory(Path.Combine(dir, "2\\22"));
    File.WriteAllText(Path.Combine(dir, "2\\22", "11.txt"),  "11");
    File.WriteAllText(Path.Combine(dir, "2\\22", "22.txt"),  "22");
    File.WriteAllText(Path.Combine(dir, "2\\22", "33.txt"),  "33");
    Directory.CreateDirectory(Path.Combine(dir, "2\\22\\222"));
    File.WriteAllText(Path.Combine(dir, "2\\22\\222", "222.txt"),  "222");
    Directory.CreateDirectory(Path.Combine(dir, "10"));
    Directory.CreateDirectory(Path.Combine(dir, "3"));
}

createTest2();
//await BulkRename.App.Program.Main(new[] {
//    Path.Combine(dir, "1"),
//    Path.Combine(dir, "2"),
//    Path.Combine(dir, "2", "1.txt"),
//    Path.Combine(dir, "2", "2.txt"),
//    Path.Combine(dir, "2\\22"),
//    Path.Combine(dir, "2\\22", "1.txt"),
//    Path.Combine(dir, "2\\22", "2.txt"),
//    Path.Combine(dir, "2\\22", "3.txt"),
//    Path.Combine(dir, "10"),
//    Path.Combine(dir, "3"),
//});
#endregion

