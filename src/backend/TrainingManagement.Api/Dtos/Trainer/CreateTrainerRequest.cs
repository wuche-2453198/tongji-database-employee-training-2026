using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Api.Dtos.Trainer;

public sealed class CreateTrainerRequest
{
    [Required]
    [MaxLength(50)]
    public string TrainerName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Title { get; set; }

    [MaxLength(100)]
    public string? Company { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [Range(typeof(decimal), "1.0", "5.0", ErrorMessage = "讲师星级范围是1.0到5.0")]
    public decimal StarLevel { get; set; }

    [RegularExpression("^[YN]$", ErrorMessage = "是否内部讲师只能是Y或N")]
    public string IsInternal { get; set; } = "Y";
}