namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.WebhookAggregate;

/// <summary>Well-known event type constants dispatched to webhook endpoints.</summary>
public static class WebhookEvents
{
    public const string UserCreated    = "user.created";
    public const string UserLogin      = "user.login";
    public const string UserLogout     = "user.logout";
    public const string TokenIssued    = "token.issued";
    public const string TokenRevoked   = "token.revoked";
    public const string ClientCreated  = "client.created";
}
