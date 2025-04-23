using System.IO;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.DAL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NetBox.Extensions;

namespace HomeCentre.PIM.Api.Controllers.Api
{
    [Route("/api/[controller]/[action]")]
    [MultiplePoliciesAuthorize(Constants.Policies.All)]
    public class FileController : BaseApiController
    {
        private readonly IFileManager _manager;
        private readonly IConfiguration _configuration;
        private readonly IDbCache _cache;

        public FileController(IFileManager manager, IConfiguration configuration, IDbCache cache)
        {
            _manager = manager;
            _configuration = configuration;
            _cache = cache;
        }

        [HttpGet]
        public async Task<int> GetMaxFileSize()
        {
            return await this._cache.GetMaxFileSize();
        }

        [HttpGet]
        public async Task<FileContentResult> DownloadFile(long fileId)
        {
            var content = await _manager.DownloadFile(fileId);
            var fileTable = await _manager.GetDetail(fileId);
            return File(content, "application/octet-stream", fileTable.Name);
        }

        [HttpPost]
        [SwaggerFileUpload]
        public async Task<FileTable> UploadFile([FromForm(Name = "uploadedFile")] IFormFile uploadedFile)
        {
            using (var stream = new MemoryStream())
            {
                await uploadedFile.CopyToAsync(stream);
                stream.Position = 0;
                var bytes = stream.ToByteArray();
                var maxFileSize = await this._cache.GetMaxFileSize();
                if (bytes.Length > maxFileSize)
                {
                    throw new HandledException($"File size should be less than {maxFileSize / (1024 * 1024)} MB.");
                }

                var fileIndex = new FileTable
                {
                    ContentLength = bytes.Length,
                    Name = uploadedFile.FileName
                };
                fileIndex.Id = await _manager.UploadFile(fileIndex, new MemoryStream(bytes));
                return fileIndex;
            }
        }
    }
}