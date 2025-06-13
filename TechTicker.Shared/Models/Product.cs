using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TechTicker.Shared.Models
{
    public class Product
    {
        [Key]
        public Guid ProductId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = null!;

        [MaxLength(100)]
        public string? Manufacturer { get; set; }

        [MaxLength(100)]
        public string? ModelNumber { get; set; }

        [MaxLength(100)]
        public string? SKU { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; } = null!;

        public string? Description { get; set; }

        public string? Specifications { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        [Required]
        public DateTimeOffset UpdatedAt { get; set; }

        [NotMapped]
        public JsonObject? SpecificationsJson
        {
            get => string.IsNullOrEmpty(Specifications)
                ? null
                : JsonNode.Parse(Specifications)?.AsObject();
            set => value?.ToJsonString();
        }
    }
}
