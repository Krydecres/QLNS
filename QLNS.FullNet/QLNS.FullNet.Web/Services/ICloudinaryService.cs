using Microsoft.AspNetCore.Http;

namespace QLNS.FullNet.Web.Services
{
    public interface ICloudinaryService
    {
        Task<string?> UploadImageAsync(IFormFile file, string folder);
    }
}