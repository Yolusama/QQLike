// See https://aka.ms/new-console-template for more information

Console.WriteLine("Rider 似乎不具备像VS那样可以直接生成不调试项目生成新的实例，故使用此方法");

const string ClientDebugPath = "QQLike/bin";
const string ClientWindowPathName = "net8.0-windows";
const string Debug = "Debug";
const string ConfigFile = "appsettings.json";
const string ProjectName = "QQLike";

var workingDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
var parentDirectory = workingDirectory.Parent;
while (parentDirectory != null)
{
    if(string.Equals(parentDirectory.Name, ProjectName, StringComparison.OrdinalIgnoreCase))
        break;
    parentDirectory = parentDirectory.Parent;
}
var directory = new  DirectoryInfo(Path.Combine(parentDirectory.FullName, ClientDebugPath));
var templateDirectory = new DirectoryInfo(Path.Combine(directory.FullName,Debug,ClientWindowPathName));

var debugDirectories = directory.GetDirectories()
    .Where(dir => dir.Name.StartsWith(Debug) && dir.Name != Debug)
    .ToList();

var filesToCopy = templateDirectory.GetFiles()
    .Where(file=> file.Name!=ConfigFile)
    .ToList();

foreach (var dir in debugDirectories)
{
    Console.WriteLine($"复制到 {dir.FullName}");
    foreach (var file in filesToCopy)
    {
        var fileInfo = new FileInfo(Path.Combine(dir.FullName, ClientWindowPathName,file.Name));
        if(!fileInfo.Exists)
            fileInfo.Create().Close();
        file.CopyTo(fileInfo.FullName, true);
    }
}

Console.WriteLine("客户端调试文件同步完毕");

