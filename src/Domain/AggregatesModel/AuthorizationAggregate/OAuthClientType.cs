using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate
{
    public static class OAuthClientType
    {
        /// <summary>Client capable of maintaining the confidentiality of its credentials (e.g. a server-side app).</summary>
        public const string Confidential = "CONFIDENTIAL";

        /// <summary>Client incapable of maintaining the confidentiality of its credentials (e.g. SPA, native app).</summary>
        public const string Public = "PUBLIC";
    }
}
