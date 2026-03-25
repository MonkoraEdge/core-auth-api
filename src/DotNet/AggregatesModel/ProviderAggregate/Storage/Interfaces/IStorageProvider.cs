namespace MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Storage.Interfaces
{
    /// <summary>
    /// Abstraction for binary/blob object storage (AWS S3, Azure Blob, GCS, MinIO, local, etc.).
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>Upload a stream as an object. Returns the object URL or key.</summary>
        Task<string> UploadAsync(
            string containerName,
            string fileName,
            Stream content,
            string contentType = "application/octet-stream",
            CancellationToken cancellationToken = default);

        /// <summary>Download an object as a stream.</summary>
        Task<Stream> DownloadAsync(
            string containerName,
            string fileName,
            CancellationToken cancellationToken = default);

        /// <summary>Delete an object.</summary>
        Task DeleteAsync(
            string containerName,
            string fileName,
            CancellationToken cancellationToken = default);

        /// <summary>Returns true if the object exists.</summary>
        Task<bool> ExistsAsync(
            string containerName,
            string fileName,
            CancellationToken cancellationToken = default);

        /// <summary>Returns the public URL for a stored object.</summary>
        string GetPublicUrl(string containerName, string fileName);
    }
}
