namespace MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Search.Interfaces
{
    /// <summary>
    /// Abstraction for full-text search engines (Elasticsearch, OpenSearch, Azure Search, etc.).
    /// </summary>
    public interface ISearchProvider
    {
        /// <summary>Index (create or replace) a document.</summary>
        Task IndexAsync<T>(
            string indexName,
            string id,
            T document,
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>Retrieve a document by ID.</summary>
        Task<T> GetAsync<T>(
            string indexName,
            string id,
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>Free-text search across the index.</summary>
        Task<IList<T>> SearchAsync<T>(
            string indexName,
            string query,
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>Delete a document by ID.</summary>
        Task DeleteAsync(
            string indexName,
            string id,
            CancellationToken cancellationToken = default);
    }
}
