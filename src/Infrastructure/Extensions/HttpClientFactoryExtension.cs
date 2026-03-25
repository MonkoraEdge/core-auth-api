using System.Diagnostics.CodeAnalysis;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.Auth.Infrastructure.Configurations;
using MonkoraEdge.Core.Auth.Infrastructure.ExternalApis;
using Microsoft.Extensions.DependencyInjection;

namespace MonkoraEdge.Core.Auth.Infrastructure.Extensions;

[ExcludeFromCodeCoverage]
public static class HttpClientFactoryExtension
{
    private const string BPBasicAuthApiClient = "BP_BASIC_AUTH_API";
    private const string GoogleApiClient = "GOOGLE_API";
    private const string GoogleOAuth2ApiClient = "GOOGLE_AOUTH2_API";
    private const string NotificationApiClient = "NOTIFICATION_API";
    private const string ResourcesApiClient = "RESOURCES_API";
    private const string FacebookClient = "FACEBOOK";
    private const string AppleClient = "APPLE";
    
    public static HttpClient CreateBPBasicAuthApiClient(this IHttpClientFactory httpClientFactory) => httpClientFactory.CreateClient(BPBasicAuthApiClient);
    public static HttpClient CreateGoogleApiClient(this IHttpClientFactory httpClientFactory) => httpClientFactory.CreateClient(GoogleApiClient);
    public static HttpClient CreateGoogleOAuth2ApiClient(this IHttpClientFactory httpClientFactory) => httpClientFactory.CreateClient(GoogleOAuth2ApiClient);
    public static HttpClient CreateNotificationClient(this IHttpClientFactory httpClientFactory) => httpClientFactory.CreateClient(NotificationApiClient);
    public static HttpClient CreateResourcesApiClient(this IHttpClientFactory httpClientFactory) => httpClientFactory.CreateClient(ResourcesApiClient);
    public static HttpClient CreateFacebookClient(this IHttpClientFactory httpClientFactory) => httpClientFactory.CreateClient(FacebookClient);
    public static HttpClient CreateAppleClient(this IHttpClientFactory httpClientFactory) => httpClientFactory.CreateClient(AppleClient);
    
    public static IServiceCollection AddCustomHttpClients(this IServiceCollection services, EnvironmentOptions options)
    {
        services.AddHttpClient(GoogleApiClient, c => { c.BaseAddress = new Uri(options.GOOGLE_API_AUTH_ENDPOINT); });
        services.AddHttpClient(GoogleOAuth2ApiClient, c => { c.BaseAddress = new Uri(options.GOOGLE_OAUTH2_API_AUTH_ENDPOINT); });
        services.AddHttpClient(NotificationApiClient, c => { c.BaseAddress = new Uri(options.NOTIFICATION_ENDPOINT); });
        services.AddHttpClient(ResourcesApiClient, c => { c.BaseAddress = new Uri(options.RESOURCE_API_ENDPOINT); });

        // Typed HTTP clients for external API integrations
        services.AddHttpClient<INotificationApi, global::MonkoraEdge.Core.Auth.Infrastructure.ExternalApis.NotificationApiClient>(
            c => c.BaseAddress = new Uri(options.NOTIFICATION_ENDPOINT));
        services.AddHttpClient<IBpApi, BpApiClient>(
            c => c.BaseAddress = new Uri(options.BP_API_ENDPOINT));

        return services;
    }
}