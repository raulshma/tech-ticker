using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TechTicker.Shared.Utilities
{
    /// <summary>
    /// Utility class for common string operations
    /// </summary>
    public static class StringUtilities
    {
        /// <summary>
        /// Converts a string to a URL-friendly slug
        /// </summary>
        public static string ToSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Convert to lowercase
            string slug = input.ToLowerInvariant();

            // Remove diacritics (accented characters)
            slug = RemoveDiacritics(slug);

            // Replace spaces and special characters with hyphens
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");

            // Trim hyphens from start and end
            slug = slug.Trim('-');

            return slug;
        }

        /// <summary>
        /// Removes diacritics (accented characters) from a string
        /// </summary>
        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var character in normalizedString)
            {
                var unicodeCategory = char.GetUnicodeCategory(character);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(character);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Masks sensitive information in a string (e.g., email addresses, phone numbers)
        /// </summary>
        public static string MaskSensitiveInfo(string input, char maskChar = '*', int visibleChars = 2)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length <= visibleChars * 2)
                return input;

            var start = input.Substring(0, visibleChars);
            var end = input.Substring(input.Length - visibleChars);
            var middle = new string(maskChar, input.Length - (visibleChars * 2));

            return $"{start}{middle}{end}";
        }

        /// <summary>
        /// Truncates a string to a specified length and adds ellipsis if necessary
        /// </summary>
        public static string Truncate(string input, int maxLength, string ellipsis = "...")
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length <= maxLength)
                return input;

            return input.Substring(0, maxLength - ellipsis.Length) + ellipsis;
        }

        /// <summary>
        /// Generates a random string of specified length
        /// </summary>
        public static string GenerateRandomString(int length, bool includeNumbers = true, bool includeSpecialChars = false)
        {
            const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string numbers = "0123456789";
            const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            var chars = letters;
            if (includeNumbers) chars += numbers;
            if (includeSpecialChars) chars += specialChars;

            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

    /// <summary>
    /// Utility class for JSON operations
    /// </summary>
    public static class JsonUtilities
    {
        private static readonly JsonSerializerOptions DefaultOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Serializes an object to JSON string
        /// </summary>
        public static string Serialize<T>(T obj, JsonSerializerOptions? options = null)
        {
            return JsonSerializer.Serialize(obj, options ?? DefaultOptions);
        }

        /// <summary>
        /// Deserializes a JSON string to an object
        /// </summary>
        public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;

            try
            {
                return JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Safely deserializes a JSON string to an object, returning a default value on failure
        /// </summary>
        public static T DeserializeOrDefault<T>(string json, T defaultValue, JsonSerializerOptions? options = null)
        {
            var result = Deserialize<T>(json, options);
            return result ?? defaultValue;
        }
    }

    /// <summary>
    /// Utility class for hash operations
    /// </summary>
    public static class HashUtilities
    {
        /// <summary>
        /// Generates an MD5 hash of the input string
        /// </summary>
        public static string GenerateMD5(string input)
        {
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        /// <summary>
        /// Generates a SHA256 hash of the input string
        /// </summary>
        public static string GenerateSHA256(string input)
        {
            using var sha256 = SHA256.Create();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        /// <summary>
        /// Generates a SHA512 hash of the input string
        /// </summary>
        public static string GenerateSHA512(string input)
        {
            using var sha512 = SHA512.Create();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha512.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }

    /// <summary>
    /// Utility class for validation operations
    /// </summary>
    public static class ValidationUtilities
    {
        /// <summary>
        /// Validates if a string is a valid email address
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates if a string is a valid phone number (basic validation)
        /// </summary>
        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Remove all non-digit characters
            var digitsOnly = Regex.Replace(phoneNumber, @"[^\d]", "");

            // Check if it has 10-15 digits (covers most international formats)
            return digitsOnly.Length >= 10 && digitsOnly.Length <= 15;
        }

        /// <summary>
        /// Validates if a string is a valid URL
        /// </summary>
        public static bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Validates if a string is a valid GUID
        /// </summary>
        public static bool IsValidGuid(string guid)
        {
            return Guid.TryParse(guid, out _);
        }
    }
    public static class ServiceDiscoveryUtilities
    {
        public static string? GetServiceEndpoint(string serviceName, string endpointName, int index = 0) =>
          Environment.GetEnvironmentVariable($"services__{serviceName}__{endpointName}__{index}");
    }
}
