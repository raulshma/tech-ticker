# TechTicker Authentication Test Script
# This script tests the authentication setup across services

# Configuration
$userServiceUrl = "https://localhost:7001"
$productServiceUrl = "https://localhost:7002"  # Adjust port as needed
$priceHistoryServiceUrl = "https://localhost:7003"  # Adjust port as needed

# Test credentials (update these to match your test data)
$testUser = "admin@techticker.com"
$testPassword = "YourPassword123!"

Write-Host "🧪 TechTicker Authentication Test Suite" -ForegroundColor Cyan
Write-Host "=" * 50

# Step 1: Test User Service Health
Write-Host "`n1️⃣  Testing User Service Health..."
try {
    $response = Invoke-RestMethod -Uri "$userServiceUrl/health" -Method GET -ErrorAction Stop
    Write-Host "✅ User Service is healthy" -ForegroundColor Green
} catch {
    Write-Host "❌ User Service is not accessible: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Obtain Access Token
Write-Host "`n2️⃣  Obtaining access token..."
$tokenBody = @{
    grant_type = "password"
    username = $testUser
    password = $testPassword
    scope = "openid profile email roles"
}

try {
    $tokenResponse = Invoke-RestMethod -Uri "$userServiceUrl/connect/token" -Method POST -Body $tokenBody -ContentType "application/x-www-form-urlencoded" -ErrorAction Stop
    $accessToken = $tokenResponse.access_token
    Write-Host "✅ Successfully obtained access token" -ForegroundColor Green
    Write-Host "   Token expires in: $($tokenResponse.expires_in) seconds"
} catch {
    Write-Host "❌ Failed to obtain access token: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Check your credentials and User Service configuration"
    exit 1
}

# Step 3: Test authenticated endpoint on User Service
Write-Host "`n3️⃣  Testing authenticated endpoint on User Service..."
$headers = @{
    Authorization = "Bearer $accessToken"
}

try {
    $userInfo = Invoke-RestMethod -Uri "$userServiceUrl/api/users/me" -Method GET -Headers $headers -ErrorAction Stop
    Write-Host "✅ Successfully accessed authenticated User Service endpoint" -ForegroundColor Green
    Write-Host "   User: $($userInfo.data.email)"
} catch {
    Write-Host "❌ Failed to access authenticated User Service endpoint: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 4: Test Product Service (if available)
Write-Host "`n4️⃣  Testing Product Service authentication..."
try {
    # Test without token first
    try {
        $response = Invoke-RestMethod -Uri "$productServiceUrl/api/product" -Method GET -ErrorAction Stop
        Write-Host "⚠️  Product Service allows unauthenticated access" -ForegroundColor Yellow
    } catch {
        if ($_.Exception.Response.StatusCode -eq 401) {
            Write-Host "✅ Product Service correctly rejects unauthenticated requests" -ForegroundColor Green
        } else {
            Write-Host "❓ Product Service response: $($_.Exception.Response.StatusCode)" -ForegroundColor Yellow
        }
    }
    
    # Test with token
    try {
        $response = Invoke-RestMethod -Uri "$productServiceUrl/api/product" -Method GET -Headers $headers -ErrorAction Stop
        Write-Host "✅ Product Service accepts authenticated requests" -ForegroundColor Green
        Write-Host "   Products count: $($response.data.items.Count)"
    } catch {
        Write-Host "❌ Product Service failed with token: $($_.Exception.Message)" -ForegroundColor Red
    }
} catch {
    Write-Host "⚠️  Product Service not accessible (may not be running)" -ForegroundColor Yellow
}

# Step 5: Test Price History Service (if available)
Write-Host "`n5️⃣  Testing Price History Service authentication..."
try {
    # Test without token first
    try {
        $testProductId = [Guid]::NewGuid()
        $response = Invoke-RestMethod -Uri "$priceHistoryServiceUrl/api/pricehistory/products/$testProductId" -Method GET -ErrorAction Stop
        Write-Host "⚠️  Price History Service allows unauthenticated access" -ForegroundColor Yellow
    } catch {
        if ($_.Exception.Response.StatusCode -eq 401) {
            Write-Host "✅ Price History Service correctly rejects unauthenticated requests" -ForegroundColor Green
        } else {
            Write-Host "❓ Price History Service response: $($_.Exception.Response.StatusCode)" -ForegroundColor Yellow
        }
    }
    
    # Test with token
    try {
        $testProductId = [Guid]::NewGuid()
        $response = Invoke-RestMethod -Uri "$priceHistoryServiceUrl/api/pricehistory/products/$testProductId" -Method GET -Headers $headers -ErrorAction Stop
        Write-Host "✅ Price History Service accepts authenticated requests" -ForegroundColor Green
    } catch {
        if ($_.Exception.Response.StatusCode -eq 404) {
            Write-Host "✅ Price History Service accepts authenticated requests (product not found is expected)" -ForegroundColor Green
        } else {
            Write-Host "❌ Price History Service failed with token: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "⚠️  Price History Service not accessible (may not be running)" -ForegroundColor Yellow
}

# Step 6: Test Token Info
Write-Host "`n6️⃣  Testing token info endpoint..."
try {
    $userInfo = Invoke-RestMethod -Uri "$userServiceUrl/connect/userinfo" -Method GET -Headers $headers -ErrorAction Stop
    Write-Host "✅ Successfully retrieved user info from token" -ForegroundColor Green
    Write-Host "   Subject: $($userInfo.sub)"
    Write-Host "   Email: $($userInfo.email)"
    if ($userInfo.role) {
        Write-Host "   Roles: $($userInfo.role -join ', ')"
    }
} catch {
    Write-Host "❌ Failed to retrieve user info: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n" + "=" * 50
Write-Host "🎯 Authentication Test Complete!" -ForegroundColor Cyan
Write-Host "`n📋 Summary:"
Write-Host "   • Update service URLs in this script to match your setup"
Write-Host "   • Ensure all services are running before testing"
Write-Host "   • Check service logs for detailed error information"
Write-Host "   • Verify authentication configuration in appsettings.json files"
