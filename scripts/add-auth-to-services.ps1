# PowerShell script to add authentication configuration to all TechTicker services

$services = @(
    "Services/TechTicker.ProductSellerMappingService",
    "Services/TechTicker.ScraperService", 
    "Services/TechTicker.ScrapingOrchestrationService",
    "Services/TechTicker.PriceNormalizationService"
)

$authConfig = @"
,
  "OpenIddictAuthentication": {
    "AuthorizationServerUrl": "https://localhost:7001",
    "Audience": "techticker-api",
    "RequireHttpsMetadata": false,
    "EnableIntrospection": false
  },
  "AuthorizationPolicies": {
    "RequireAuthenticatedUserByDefault": true,
    "DefaultScheme": "OpenIddict",
    "Policies": {
      "ServiceManagement": {
        "RequiredRoles": ["Admin"],
        "RequiredScopes": ["write"]
      },
      "ServiceRead": {
        "RequiredRoles": ["User", "Admin"],
        "RequiredScopes": ["read"]
      }
    }
  }
"@

foreach ($service in $services) {
    $appsettingsPath = "$service/appsettings.Development.json"
    
    if (Test-Path $appsettingsPath) {
        Write-Host "Updating $appsettingsPath..."
        
        # Read the current content
        $content = Get-Content $appsettingsPath -Raw
        
        # Check if authentication is already configured
        if ($content -notlike "*OpenIddictAuthentication*") {
            # Remove the closing brace and add auth config
            $content = $content.TrimEnd().TrimEnd('}')
            $content += $authConfig + "`n}"
            
            # Write back to file
            Set-Content -Path $appsettingsPath -Value $content
            Write-Host "✅ Added authentication config to $service"
        } else {
            Write-Host "⚠️  Authentication already configured for $service"
        }
    } else {
        Write-Host "❌ File not found: $appsettingsPath"
    }
}

Write-Host "`n🔄 Next steps:"
Write-Host "1. Update each service's Program.cs to use AddTechTickerShared with authentication"
Write-Host "2. Update pipeline configuration to use UseTechTickerPipeline"
Write-Host "3. Add authorization attributes to controllers"
Write-Host "4. Test authentication with each service"
