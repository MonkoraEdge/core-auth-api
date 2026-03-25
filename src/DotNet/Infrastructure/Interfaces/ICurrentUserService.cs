using System.Security.Claims;

namespace MonkoraEdge.Core.DotNet.Infrastructure.Interfaces
{
    /// <summary>
    /// Abstraction for accessing the currently authenticated user.
    /// Decouples infrastructure from IHttpContextAccessor, making it testable.
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>Subject / NameIdentifier claim value.</summary>
        string UserId { get; }

        /// <summary>Name claim value.</summary>
        string UserName { get; }

        /// <summary>Email claim value.</summary>
        string Email { get; }

        /// <summary>Primary role claim value.</summary>
        string Role { get; }

        /// <summary>Returns true when the current request is authenticated.</summary>
        bool IsAuthenticated { get; }

        /// <summary>All claims of the current principal.</summary>
        IEnumerable<Claim> Claims { get; }
    }
}
