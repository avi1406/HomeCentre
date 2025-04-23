using System.Web;
using Azure.Storage.Sas;
using HomeCentre.PIM.Common;

namespace HomeCentre.PIM.BAL.Implementations
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Azure.Storage;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using HomeCentre.PIM.BAL.Model;
    using HomeCentre.PIM.Common.Interfaces;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.Extensions.Configuration;

    public class BlobRepo : IBlobRepo
    {
        private readonly IConfiguration _configuration;

        public BlobRepo(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// copying a blob file from a source container to destination container 
        /// </summary>
        /// <param name="destinationFilename"></param>
        /// <param name="containerName"></param>
        /// <param name="sourceUrl"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> CopyFile(string destinationFilename, string containerName, string sourceUrl)
        {
            if (destinationFilename == null || containerName == null)
            {
                throw new Exception(" Error on CopyFile : input parameter is null");
            }

            try
            {
                var connectionString = this._configuration["AzureBlobStorageKey"];
                var blobClient = new BlobClient(connectionString, containerName, sourceUrl);
                var copiedFile = await blobClient.StartCopyFromUriAsync(new Uri(destinationFilename));
                var newfile = new CopyFromUriOperation(copiedFile.Id, blobClient);
                await newfile.WaitForCompletionAsync();
                return blobClient.Uri.ToString();
            }
            catch (Exception e)
            {
                throw new Exception($"Error on CopyFile : {e.Message}");
            }
        }

        /// <summary>
        /// Delete a blob File 
        /// </summary>
        /// <param name="shareName"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task DeleteFile(string shareName, string fileName)
        {
            if (shareName == null || fileName == null)
            {
                throw new Exception("Error on DeleteFile : input parameter is null");
            }

            try
            {
                var blobName = Path.GetFileNameWithoutExtension(shareName);
                var blobNameWithFileExtension = Path.GetFileNameWithoutExtension(fileName);
                var connectionString = this._configuration["AzureBlobStorageKey"];
                var blobClient = new BlobClient(connectionString, shareName, fileName);
                await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception e)
            {
                throw new Exception($"Error on DeleteFile : {e.Message}");
            }
        }

        /// <summary>
        /// Download a blob File 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> DownloadFile(string url)
        {
            if (url == null)
            {
                throw new Exception("Error on DownloadFile : input parameter is null");
            }

            try
            {
                var filePath = Path.ChangeExtension(Path.GetTempFileName(), url.GetFileExtensionFromUrl());
                var (accountName, accountKey) = GetAccountDetails();
                var storageSharedKeyCredential = new StorageSharedKeyCredential(accountName, accountKey);
                var blobClient = new BlobClient(new Uri(url.Split('?')[0]), storageSharedKeyCredential);
                await blobClient.DownloadToAsync(filePath, default);
                return filePath;
            }
            catch (Exception e)
            {
                throw new Exception($"Error on DownloadFile : {e.Message}");
            }
        }

        public string GetLastModifiedFileUrl(string fileName, string containerName)
        {
            try
            {
                var connectionString = this._configuration["AzureBlobStorageKey"];
                var backupStorageAccount = CloudStorageAccount.Parse(connectionString);

                var backupBlobClient = backupStorageAccount.CreateCloudBlobClient();
                var backupContainer = backupBlobClient.GetContainerReference(containerName);

                // var fileList = new List<string>();
                var blobs = backupContainer.ListBlobs().OfType<CloudBlockBlob>().ToList();

                // var fileList = (from blob in blobs
                //                where blob.Name.ToLower().Contains(fileName.ToLower())
                //                select new AzureBlobRepoModel
                //                {
                //                    Name = blob.Name,
                //                    ModifiedDate = (DateTimeOffset)blob.Properties.LastModified,
                //                    Url = blob.Uri.ToString()
                //                })
                //    .OrderByDescending(a => a.ModifiedDate);
                // return fileList.FirstOrDefault().Url;
                CloudBlockBlob selectedBlob = null;

                if (fileName.Equals("Class", StringComparison.OrdinalIgnoreCase))
                {
                    selectedBlob = blobs.FirstOrDefault(blob => blob.Name.Equals("Class.xlsx", StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    selectedBlob = blobs
                        .Where(blob => blob.Name.ToLower().Contains(fileName.ToLower()))
                        .OrderByDescending(blob => blob.Properties.LastModified)
                        .FirstOrDefault();
                }

                if (selectedBlob != null)
                {
                    return selectedBlob.Uri.ToString();
                }
                else
                {
                    return "Blob not found";
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error on GetLastModifedFileUrl : {e.Message}");
            }
        }

        /// <summary>
        /// Checks Whether the Selected BlobFileName is already exists or Not in a container
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="containerName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> IfFileExist(string filename, string containerName)
        {
            if (filename == null || containerName == null)
            {
                throw new Exception("Error on IfFileExist : The File Name Does Not Exist");
            }

            try
            {
                var connectionString = this._configuration["AzureBlobStorageKey"];
                var blobServiceClient = new BlobServiceClient(connectionString);

                var container = blobServiceClient.GetBlobContainerClient(containerName);

                var blob = container.GetBlobClient(filename);

                return blob.Exists();
            }
            catch (Exception e)
            {
                throw new Exception($"Error on IfFileExist : {e.Message}");
            }
        }

        /// <summary>
        /// Upload a file with a specified blobFileName and ContainerName
        /// </summary>
        /// <param name="blobFileName"></param>
        /// <param name="containerName"></param>
        /// <param name="localFilePath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> UploadFile(string blobFileName, string containerName, string localFilePath)
        {
            if (blobFileName == null || containerName == null || localFilePath == null)
            {
                throw new Exception("Error on UploadFile : input parameter is null");
            }

            try
            {
                var connectionString = this._configuration["AzureBlobStorageKey"];
                var blobServiceClient = new BlobServiceClient(connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobFileName + Path.GetExtension(localFilePath));
                var response = await blobClient.UploadAsync(localFilePath, true);
                return blobClient.Uri.ToString();
            }
            catch (Exception e)
            {
                throw new Exception($"Error on UploadFile : {e.Message}");
            }
        }

        public async Task<string> GetPublicAccessWriteUrl(string containerName, string fileName,
            int timeOutInMinutes = 10)
        {
            return await Task.Run(() =>
            {
                var (accountName, accountKey) = GetAccountDetails();
                var storageSharedKeyCredential =
                    new StorageSharedKeyCredential(accountName, accountKey);
                var blobSasBuilder = new BlobSasBuilder();
                blobSasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write |
                                              BlobSasPermissions.Read);
                blobSasBuilder.StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5);
                blobSasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(timeOutInMinutes);
                blobSasBuilder.BlobName = fileName;
                blobSasBuilder.BlobContainerName = containerName;
                var sasQueryParameters = blobSasBuilder.ToSasQueryParameters(storageSharedKeyCredential);
                var fullUri = new UriBuilder()
                {
                    Scheme = "https",
                    Host = $"{accountName}.blob.core.windows.net",
                    Path = $"{containerName}/{fileName}",
                    Query = sasQueryParameters.ToString()
                };

                return fullUri.Uri.ToString();
            });
        }

        public async Task<string> GetPublicAccessWriteUrl(string url, int timeOutInMinutes = 10)
        {
            return await Task.Run(() =>
            {
                var (accountName, accountKey) = GetAccountDetails();
                var storageSharedKeyCredential =
                    new StorageSharedKeyCredential(accountName, accountKey);
                var blobClient = new BlobClient(new Uri(url.Split('?').First()), storageSharedKeyCredential);
                var containerName = blobClient.BlobContainerName;
                var fileName = blobClient.Name;
                var blobSasBuilder = new BlobSasBuilder();
                blobSasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write |
                                              BlobSasPermissions.Read);
                blobSasBuilder.StartsOn = DateTimeOffset.UtcNow;
                blobSasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(timeOutInMinutes);
                blobSasBuilder.BlobName = fileName;
                blobSasBuilder.BlobContainerName = containerName;
                var sasQueryParameters = blobSasBuilder.ToSasQueryParameters(storageSharedKeyCredential);
                var fullUri = new UriBuilder()
                {
                    Scheme = "https",
                    Host = $"{accountName}.blob.core.windows.net",
                    Path = $"{containerName}/{fileName}",
                    Query = sasQueryParameters.ToString()
                };

                return fullUri.Uri.ToString();
            });
        }

        public async Task<string> GetPublicAccessReadUrl(string containerName, string fileName, int expiryInMinutes = 2)
        {
            return await Task.Run(() =>
            {
                var (accountName, accountKey) = GetAccountDetails();
                var storageSharedKeyCredential =
                    new StorageSharedKeyCredential(accountName, accountKey);
                var blobClient = new BlobClient(this._configuration["StorageConnectionString"], containerName, fileName);
                var blobSasBuilder = new BlobSasBuilder();
                blobSasBuilder.SetPermissions(BlobSasPermissions.Read);
                blobSasBuilder.StartsOn = DateTimeOffset.UtcNow;
                blobSasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(10);
                blobSasBuilder.BlobName = HttpUtility.UrlDecode(blobClient.Name);
                blobSasBuilder.BlobContainerName = blobClient.BlobContainerName;
                blobSasBuilder.CacheControl = "max-age=0";
                var sasQueryParameters = blobSasBuilder.ToSasQueryParameters(storageSharedKeyCredential);
                var fullUri = new UriBuilder
                {
                    Scheme = "https",
                    Host = $"{accountName}.blob.core.windows.net",
                    Path = $"{blobClient.BlobContainerName}/{HttpUtility.UrlDecode(blobClient.Name)}",
                    Query = sasQueryParameters.ToString()
                };

                return fullUri.Uri.ToString();
            });
        }

        public async Task<string> GetPublicAccessReadUrl(string documentLink, int expiryInMinutes = 10)
        {
            return await Task.Run(() =>
            {
                var (accountName, accountKey) = GetAccountDetails();
                var storageSharedKeyCredential =
                    new StorageSharedKeyCredential(accountName, accountKey);
                var blobClient = new BlobClient(new Uri(documentLink.Split('?').First()), storageSharedKeyCredential);
                var blobSasBuilder = new BlobSasBuilder();
                blobSasBuilder.SetPermissions(BlobSasPermissions.Read);
                blobSasBuilder.StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5);
                blobSasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryInMinutes);
                blobSasBuilder.BlobName = HttpUtility.UrlDecode(blobClient.Name);
                blobSasBuilder.BlobContainerName = blobClient.BlobContainerName;
                blobSasBuilder.CacheControl = "max-age=0";
                var sasQueryParameters = blobSasBuilder.ToSasQueryParameters(storageSharedKeyCredential);
                var fullUri = new UriBuilder()
                {
                    Scheme = "https",
                    Host = $"{accountName}.blob.core.windows.net",
                    Path = $"{blobClient.BlobContainerName}/{HttpUtility.UrlDecode(blobClient.Name)}",
                    Query = sasQueryParameters.ToString()
                };

                return fullUri.Uri.ToString();
            });
        }

        /// <summary>
        /// Get AccountName and AccountKey from connection_string
        /// </summary>
        private (string, string) GetAccountDetails()
        {
            var connectionString = this._configuration["AzureBlobStorageKey"];
            var azureConnectionString = connectionString;
            var items = azureConnectionString.Split(';');
            var dictionary = items.Select(item => item.Split('='))
                .ToDictionary(split => split[0], split => string.Join("=", split.Skip(1)));
            return (dictionary["AccountName"], dictionary["AccountKey"]);
        }
    }
}