using Google.Apis.Auth.OAuth2;

namespace MonkoraEdge.Core.DotNet.Utilities
{
    public static class CredentialHelper
    {
        public static GoogleCredential CreateGoogleCredential(string jsonSingleLine)
        {
            var credentialString = jsonSingleLine
                .Replace("<double-quote>", "\"")
                .Replace("<comma>", ",")
                .Replace("<newline>", "\n");

            return GoogleCredential.FromServiceAccountCredential(
                ServiceAccountCredential.FromServiceAccountData(
                    new MemoryStream(System.Text.Encoding.UTF8.GetBytes(credentialString))));

        }
    }
}
