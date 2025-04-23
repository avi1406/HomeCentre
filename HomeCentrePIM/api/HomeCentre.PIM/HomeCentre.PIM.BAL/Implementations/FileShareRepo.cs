using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Files.Shares;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.Common.Interfaces;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.File;
using Microsoft.Extensions.Configuration;

namespace HomeCentre.PIM.BAL.Implementations
{
    public class FileShareRepo : IBlobRepo
    {
        private readonly IConfiguration _configuration;

        public FileShareRepo(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> UploadFile(string blobFileName, string containerName, string localFilePath)
        {
            if (blobFileName == null || containerName == null || localFilePath == null)
            {
                throw new Exception("Error on UploadFile : Input Parameter is Null");
            }

            try
            {
                var connectionString = this._configuration["AzureBlobStorageKey"];
                ShareClient shareclient = new ShareClient(connectionString, containerName);
                var fileExtention = Path.GetExtension(localFilePath);
                ShareDirectoryClient directory = shareclient.GetRootDirectoryClient();
                var fileNameExtention = blobFileName + fileExtention;
                ShareFileClient file = directory.GetFileClient(fileNameExtention);
                Stream stream = File.OpenRead(localFilePath);
                file.Create(stream.Length);
                file.UploadRange(
                        new HttpRange(0, stream.Length),
                        stream);
                return file.Uri.ToString();
            }
            catch (Exception e)
            {
                throw new Exception($"Error on UploadFile : {e.Message}");
            }
        }

        public async Task<string> DownloadFile(string url)
        {
            if (url == null)
            {
                throw new ArgumentNullException("Error on DownloadFile : Input Parameter is Null");
            }

            try
            {
                string connectionString = this._configuration["AzureBlobStorageKey"];
                string downloadPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
                Directory.CreateDirectory(downloadPath);
                var filepath = Path.GetFileName(url);
                string filePath = downloadPath + "\\" + filepath;
                var (accountName, accountKey) = GetAccountDetails();
                StorageCredentials accountCredentials = new StorageCredentials(accountName, accountKey);
                var client = new CloudFile(new Uri(url), accountCredentials);
                client.DownloadToFile(filePath, System.IO.FileMode.OpenOrCreate);
                return filePath;
            }
            catch (Exception e)
            {
                throw new Exception($"Error on DownloadFile : {e.Message}");
            }
        }

        public async Task DeleteFile(string shareName, string fileName)
        {
            if (shareName == null || fileName == null)
            {
                throw new Exception("Error on DeleteFile : Input Parameter is Null");
            }

            try
            {
                var connectionString = this._configuration["AzureBlobStorageKey"];
                CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
                CloudFileClient client = account.CreateCloudFileClient();

                // get File Share
                CloudFileShare cloudFileShare = client.GetShareReference(shareName);

                // get the related directory
                CloudFileDirectory root = cloudFileShare.GetRootDirectoryReference();
                CloudFileDirectory directory = root.GetDirectoryReference("test");

                // get the file reference
                CloudFile file = directory.GetFileReference(fileName);
                await file.DeleteAsync();
            }
            catch (Exception e)
            {
                throw new Exception($"Error on DeleteFile : {e.Message}");
            }
        }

        public async Task<string> CopyFile(string destinationFilename, string containerName, string sourceUrl)
        {
            if (destinationFilename == null || containerName == null || sourceUrl == null)
            {
                throw new Exception("Error on CopyFile : Input Parameter is Null");
            }

            try
            {
                var connectionString = this._configuration["AzureBlobStorageKey"];
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
                CloudFileClient cloudFileClient = cloudStorageAccount.CreateCloudFileClient();
                CloudFileShare cloudFileShare = cloudFileClient.GetShareReference(containerName);
                CloudFileDirectory directory = cloudFileShare.GetRootDirectoryReference();
                CloudFileDirectory additionalDirector = directory.GetDirectoryReference("test");
                CloudFile file = additionalDirector.GetFileReference(destinationFilename);
                await file.StartCopyAsync(new Uri(sourceUrl));
                var url = file.Uri.ToString();
                return url;
            }
            catch (Exception e)
            {
                throw new Exception($"Error on CopyFile : {e.Message}");
            }
        }

        public string GetLastModifiedFileUrl(string fileName, string containerName)
        {
            if (fileName == null || containerName == null)
            {
                throw new Exception("Error on GetLastModifiedFileUrl : Input parameter is Null");
            }

            try
            {
                var connectionString = this._configuration["AzureBlobStorageKey"];
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
                CloudFileClient client = cloudStorageAccount.CreateCloudFileClient();
                CloudFileShare filename = client.GetShareReference(containerName);
                CloudFileDirectory root = filename.GetRootDirectoryReference();
                var listfile = root.ListFilesAndDirectories().ToList();
                var model = new List<AzureBlobRepoModel>();
                foreach (var file in listfile)
                {
                    var blobmodel = new AzureBlobRepoModel();
                    var url = Path.GetFileName(file.Uri.ToString());
                    CloudFile cloud = root.GetFileReference(url);
                    cloud.FetchAttributes();
                    blobmodel.Name = url;
                    blobmodel.ModifiedDate = (DateTimeOffset)cloud.Properties.LastModified;
                    blobmodel.Url = file.Uri.ToString();
                    model.Add(blobmodel);
                }

                var query = (from properties in model
                             where properties.Name.ToLower().Contains(fileName.ToLower())
                             select new AzureBlobRepoModel
                             {
                                 Name = properties.Name,
                                 ModifiedDate = (DateTimeOffset)properties.ModifiedDate,
                                 Url = properties.Url.ToString()
                             }).OrderByDescending(a => a.ModifiedDate);
                return query.FirstOrDefault().Url;
            }
            catch (Exception e)
            {
                throw new Exception($"Error on GetLastModifiedFileUrl : {e.Message}");
            }
        }

        public async Task<bool> IfFileExist(string filename, string containerName)
        {
            if (filename == null || containerName == null)
            {
                throw new Exception("Error on IfFileExist : Enter the valid Details");
            }

            try
            {
                var connectionString = this._configuration["AzureBlobStorageKey"];
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
                CloudFileClient cloudFileClient = cloudStorageAccount.CreateCloudFileClient();
                CloudFileShare cloudFileShare = cloudFileClient.GetShareReference(containerName);
                CloudFileDirectory directory = cloudFileShare.GetRootDirectoryReference();
                CloudFile file = directory.GetFileReference(filename);
                return file.Exists();
            }
            catch (Exception e)
            {
                throw new Exception($"Error on IfFileExist : {e.Message}");
            }
        }

        public Task<string> GetPublicAccessWriteUrl(string containerName, string fileName, int timeOutInMinutes = 10)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetPublicAccessWriteUrl(string url, int timeOutInMinutes = 10)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetPublicAccessReadUrl(string containerName, string fileName, int expiryInMinutes = 2)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetPublicAccessReadUrl(string documentLin, int expiryInMinutes = 2)
        {
            throw new NotImplementedException();
        }

        private (string, string) GetAccountDetails()
        {
            var connectionString = this._configuration["AzureBlobStorageKey"];
            var azureConnectionString = connectionString;
            var items = azureConnectionString.Split(';');
            var dictionary = items.Select(item => item.Split('=')).ToDictionary(split => split[0], split => string.Join("=", split.Skip(1)));
            return (dictionary["AccountName"], dictionary["AccountKey"]);
        }
    }
}