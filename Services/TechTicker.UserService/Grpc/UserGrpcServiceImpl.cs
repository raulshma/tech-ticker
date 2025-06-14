using Grpc.Core;
using TechTicker.Grpc.Contracts.Users;
using TechTicker.UserService.Services;
using Google.Protobuf.WellKnownTypes;

namespace TechTicker.UserService.Grpc
{
    /// <summary>
    /// gRPC service implementation for User operations
    /// </summary>
    public class UserGrpcServiceImpl : UserGrpcService.UserGrpcServiceBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserGrpcServiceImpl> _logger;

        public UserGrpcServiceImpl(IUserService userService, ILogger<UserGrpcServiceImpl> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        public override async Task<UserResponse> GetUser(GetUserRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.UserId, out var userId))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format"));
                }

                var result = await _userService.GetUserByIdAsync(userId);
                
                if (!result.IsSuccess)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, result.ErrorMessage ?? "User not found"));
                }

                return MapToGrpcUserResponse(result.Data!);
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user via gRPC: {UserId}", request.UserId);
                throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
            }
        }

        public override async Task<UserResponse> GetUserByEmail(GetUserByEmailRequest request, ServerCallContext context)
        {
            try
            {
                var result = await _userService.GetUserByEmailAsync(request.Email);
                
                if (!result.IsSuccess)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, result.ErrorMessage ?? "User not found"));
                }

                return MapToGrpcUserResponse(result.Data!);
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email via gRPC: {Email}", request.Email);
                throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
            }
        }

        public override async Task<ValidateUserResponse> ValidateUser(ValidateUserRequest request, ServerCallContext context)
        {
            try
            {
                var result = await _userService.ValidateUserAsync(request.Email, request.Password);
                
                var response = new ValidateUserResponse
                {
                    IsValid = result.IsSuccess
                };

                if (result.IsSuccess && result.Data != null)
                {
                    response.User = MapToGrpcUserResponse(result.Data);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user via gRPC: {Email}", request.Email);
                throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
            }
        }

        public override async Task<UserExistsResponse> UserExists(UserExistsRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.UserId, out var userId))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format"));
                }

                var exists = await _userService.UserExistsAsync(userId);
                
                return new UserExistsResponse
                {
                    Exists = exists
                };
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user existence via gRPC: {UserId}", request.UserId);
                throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
            }
        }

        public override async Task<GetUserPermissionsResponse> GetUserPermissions(GetUserPermissionsRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.UserId, out var userId))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format"));
                }

                var permissionsResult = await _userService.GetUserPermissionsAsync(userId);
                var rolesResult = await _userService.GetUserRolesAsync(userId);

                var response = new GetUserPermissionsResponse();

                if (permissionsResult.IsSuccess && permissionsResult.Data != null)
                {
                    response.Permissions.AddRange(permissionsResult.Data);
                }

                if (rolesResult.IsSuccess && rolesResult.Data != null)
                {
                    response.Roles.AddRange(rolesResult.Data);
                }

                return response;
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user permissions via gRPC: {UserId}", request.UserId);
                throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
            }
        }

        private static UserResponse MapToGrpcUserResponse(DTOs.UserResponse user)
        {
            var response = new UserResponse
            {
                UserId = user.UserId.ToString(),
                Email = user.Email,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                IsActive = user.IsActive,
                CreatedAt = Timestamp.FromDateTimeOffset(user.CreatedAt),
                UpdatedAt = Timestamp.FromDateTimeOffset(user.UpdatedAt)
            };

            response.Roles.AddRange(user.Roles);

            return response;
        }
    }
}
