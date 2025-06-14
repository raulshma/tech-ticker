namespace TechTicker.Shared.Configuration
{
    /// <summary>
    /// OpenIddict authentication configuration settings
    /// </summary>
    public class OpenIddictAuthenticationSettings
    {
        public const string SectionName = "OpenIddictAuthentication";
        
        /// <summary>
        /// The base URL of the authorization server (User Service)
        /// </summary>
        public string AuthorizationServerUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// The audience for token validation
        /// </summary>
        public string Audience { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether to require HTTPS metadata
        /// </summary>
        public bool RequireHttpsMetadata { get; set; } = true;
        
        /// <summary>
        /// The client ID for inter-service communication
        /// </summary>
        public string ClientId { get; set; } = string.Empty;
        
        /// <summary>
        /// The client secret for inter-service communication
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;
        
        /// <summary>
        /// Cache duration for JWKS in minutes
        /// </summary>
        public int JwksCacheDurationMinutes { get; set; } = 60;
        
        /// <summary>
        /// Enable or disable token introspection endpoint calls
        /// </summary>
        public bool EnableIntrospection { get; set; } = false;
    }

    /// <summary>
    /// JWT Bearer authentication configuration settings
    /// </summary>
    public class JwtBearerSettings
    {
        public const string SectionName = "JwtBearer";

        /// <summary>
        /// The JWT token issuer (authority)
        /// </summary>
        public string Authority { get; set; } = string.Empty;

        /// <summary>
        /// The audience for JWT token validation
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Whether to require HTTPS for metadata
        /// </summary>
        public bool RequireHttpsMetadata { get; set; } = true;

        /// <summary>
        /// The metadata address for OIDC discovery
        /// </summary>
        public string MetadataAddress { get; set; } = string.Empty;

        /// <summary>
        /// Whether to validate the issuer signing key
        /// </summary>
        public bool ValidateIssuerSigningKey { get; set; } = true;

        /// <summary>
        /// The signing key for JWT validation (for development only)
        /// </summary>
        public string SigningKey { get; set; } = string.Empty;

        /// <summary>
        /// Token validation timeout in seconds
        /// </summary>
        public int TokenValidationTimeoutSeconds { get; set; } = 30;
    }
    
    /// <summary>
    /// Authorization policy configuration
    /// </summary>
    public class AuthorizationPolicySettings
    {
        public const string SectionName = "AuthorizationPolicies";
        
        /// <summary>
        /// Default authentication scheme to use
        /// </summary>
        public string DefaultScheme { get; set; } = "OpenIddict";
        
        /// <summary>
        /// Whether to require authenticated users by default
        /// </summary>
        public bool RequireAuthenticatedUserByDefault { get; set; } = true;
        
        /// <summary>
        /// Custom authorization policies
        /// </summary>
        public Dictionary<string, PolicyDefinition> Policies { get; set; } = new();
    }
    
    /// <summary>
    /// Definition for a custom authorization policy
    /// </summary>
    public class PolicyDefinition
    {
        /// <summary>
        /// Required roles for this policy
        /// </summary>
        public List<string> RequiredRoles { get; set; } = new();
        
        /// <summary>
        /// Required claims for this policy
        /// </summary>
        public Dictionary<string, string> RequiredClaims { get; set; } = new();
        
        /// <summary>
        /// Required scopes for this policy
        /// </summary>
        public List<string> RequiredScopes { get; set; } = new();
    }
}
