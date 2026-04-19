using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Coffee.Services
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration config)
        {
            // ✅ Cách 2: GetSection (ổn định khi deploy Render)
            var cloudinaryConfig = config.GetSection("Cloudinary");

            var account = new Account(
                cloudinaryConfig["CloudName"],
                cloudinaryConfig["ApiKey"],
                cloudinaryConfig["ApiSecret"]
            );

            _cloudinary = new Cloudinary(account);
        }

        // =========================
        // 📤 UPLOAD IMAGE
        // =========================
        public async Task<string?> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),

                // 📁 giống wwwroot/img/Products nhưng trên cloud
                Folder = "img/Products"
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            return result?.SecureUrl?.ToString();
        }
    }
}