using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model.FileProcessor;
using HomeCentre.PIM.DAL;
using Microsoft.Extensions.Configuration;
using Renci.SshNet;
using FileInfo = HomeCentre.PIM.BAL.Model.FileProcessor.FileInfo;

namespace HomeCentre.PIM.BAL.Implementations.FileProcessor;

public class SftpExplorer : IFileExplorer
{
    private readonly IConfiguration _configuration;

    public SftpExplorer(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<DirInfo> GetDirectory(FileConfiguration fileConfiguration)
    {
        var connectionInfo = new ConnectionInfo(_configuration["SFTPHost"],
            _configuration["SFTPUsername"],
            new PasswordAuthenticationMethod(_configuration["SFTPUsername"], _configuration["SFTPPassword"]));
        var sftpHomePath = _configuration["SFTPHomePath"];
        using var client = new SftpClient(connectionInfo);
        client.Connect();
        var ar = client.BeginListDirectory(sftpHomePath, null, null);
        var result = client.EndListDirectory(ar);
        var folder = result.FirstOrDefault(x => x.IsDirectory && x.Name == fileConfiguration.TargetTable);
        if (folder == null)
        {
            client.Disconnect();
            throw new DirectoryNotFoundException(fileConfiguration.TargetTable);
        }

        client.Disconnect();
        return new DirInfo
        {
            Name = folder.Name,
            Path = folder.FullName
        };
    }

    public async Task<IEnumerable<FileInfo>> GetFiles(FileConfiguration fileConfiguration)
    {
        var connectionInfo = new ConnectionInfo(_configuration["SFTPHost"],
            _configuration["SFTPUsername"],
            new PasswordAuthenticationMethod(_configuration["SFTPUsername"], _configuration["SFTPPassword"]));
        var sftpHomePath = $"{_configuration["SFTPHomePath"]}/{fileConfiguration.TargetTable}";
        using var client = new SftpClient(connectionInfo);
        client.Connect();
        var ar = client.BeginListDirectory(_configuration["SFTPHomePath"], null, null);
        var result = client.EndListDirectory(ar).ToList();
        var folder = result.FirstOrDefault(x => x.IsDirectory && x.Name == fileConfiguration.TargetTable);
        if (folder == null)
        {
            client.Disconnect();
            throw new DirectoryNotFoundException(fileConfiguration.TargetTable);
        }

        ar = client.BeginListDirectory(sftpHomePath, null, null);
        result = client.EndListDirectory(ar).ToList();
        var files = result.Where(x => !x.IsDirectory);
        var list = new List<FileInfo>();
        var dir = new DirInfo
        {
            Name = folder.Name,
            Path = folder.FullName
        };
        foreach (var file in files)
        {
            list.Add(new FileInfo
            {
                Dir = dir,
                Name = file.Name,
                Path = file.FullName
            });
        }

        client.Disconnect();

        return list;
    }

    public async Task<Stream> GetFileContent(FileInfo filePage, FileConfiguration fileConfiguration)
    {
        var connectionInfo = new ConnectionInfo(_configuration["SFTPHost"],
            _configuration["SFTPUsername"],
            new PasswordAuthenticationMethod(_configuration["SFTPUsername"], _configuration["SFTPPassword"]));
        using var client = new SftpClient(connectionInfo);
        client.Connect();
        var stream = new MemoryStream();
        client.DownloadFile(filePage.Path, stream);
        client.Disconnect();
        return stream;
    }

    public async Task ArchiveFile(FileInfo filePage)
    {
        var connectionInfo = new ConnectionInfo(_configuration["SFTPHost"],
            _configuration["SFTPUsername"],
            new PasswordAuthenticationMethod(_configuration["SFTPUsername"], _configuration["SFTPPassword"]));
        using var client = new SftpClient(connectionInfo);
        client.Connect();
        var outputDir = $"{filePage.Dir.Path}/archive";
        if (!client.Exists(outputDir))
        {
            client.CreateDirectory(outputDir);
        }

        var inFile = client.Get(filePage.Path);
        var outFilePath = $"{outputDir}/{filePage.Name}";
        if (client.Exists(outFilePath))
        {
            client.DeleteFile(outFilePath);
        }

        inFile.MoveTo($"{outputDir}/{filePage.Name}");
        client.Disconnect();
    }
}