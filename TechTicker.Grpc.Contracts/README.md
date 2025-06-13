# TechTicker gRPC Contracts

This project contains the shared gRPC contract definitions (.proto files) for inter-service communication in the TechTicker ecosystem.

## Overview

The gRPC contracts define strongly-typed APIs for efficient inter-service communication while maintaining REST APIs for external consumers. This hybrid approach provides:

- **Performance**: Binary protocol for internal service communication
- **Type Safety**: Strongly-typed contracts prevent runtime errors
- **Versioning**: Explicit contract versioning and backward compatibility
- **Streaming**: Support for real-time data streaming between services

## Contract Definitions

### products.proto
Defines the ProductGrpcService for product and category operations:
- Product CRUD operations
- Category CRUD operations  
- Batch operations for multiple products/categories
- Real-time product update streaming

### mappings.proto
Defines the MappingGrpcService for product seller mapping operations:
- Mapping retrieval and filtering
- Active mappings for orchestrator
- Scraping timestamp updates
- Real-time mapping update streaming

### users.proto
Defines the UserGrpcService for user and authentication operations:
- User retrieval and validation
- Permission and role management
- Authentication support

## Generated Code

The proto files generate C# classes and service definitions that are used by:
- **gRPC Servers**: Implement the service interfaces
- **gRPC Clients**: Call remote services with strongly-typed methods

## Usage in Services

### Server Implementation
```csharp
public class ProductGrpcServiceImpl : ProductGrpcService.ProductGrpcServiceBase
{
    public override async Task<ProductResponse> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        // Implementation
    }
}
```

### Client Usage
```csharp
public class SomeService
{
    private readonly ProductGrpcService.ProductGrpcServiceClient _productClient;
    
    public async Task<ProductResponse> GetProductAsync(string productId)
    {
        var request = new GetProductRequest { ProductId = productId };
        return await _productClient.GetProductAsync(request);
    }
}
```

## Benefits

1. **Performance**: gRPC uses HTTP/2 and binary serialization
2. **Type Safety**: Compile-time validation of service contracts
3. **Language Agnostic**: Proto definitions work across programming languages
4. **Streaming**: Built-in support for streaming data
5. **Load Balancing**: Built-in client-side load balancing
6. **Retries**: Automatic retry policies for failed calls

## Best Practices

1. **Backward Compatibility**: Only add new fields, never remove or change existing ones
2. **Versioning**: Use package versioning for breaking changes
3. **Documentation**: Keep proto files well-documented
4. **Field Numbers**: Never reuse field numbers
5. **Naming**: Use snake_case for field names (converted to PascalCase in C#)

## Integration with Services

This package is referenced by all services that need inter-service communication:
- ProductService (implements ProductGrpcService)
- ProductSellerMappingService (implements MappingGrpcService)
- UserService (implements UserGrpcService)
- Orchestrator (consumes all services)

## Development

### Adding New Contracts
1. Create new .proto file in the Protos directory
2. Define service and message types
3. Update package references in consuming services
4. Implement server-side handlers
5. Update client code to use new contracts

### Testing
Use tools like:
- **grpcurl**: Command-line tool for testing gRPC services
- **Postman**: GUI tool with gRPC support
- **BloomRPC**: Dedicated gRPC client

## Deployment

The contracts package is versioned and can be:
- Published to internal NuGet feed
- Shared as git submodule
- Distributed via CI/CD pipeline
