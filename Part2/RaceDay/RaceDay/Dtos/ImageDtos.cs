namespace RaceDay.Dtos
{
    /// <summary>
    /// Returned after an image is uploaded. Url is the public Azure Blob Storage address.
    /// </summary>
    public class ImageUploadResponse
    {
        public string Url { get; set; } = string.Empty;
    }
}
