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

    [Range(typeof(decimal), "1.0", "5.0", ErrorMessage = "璁插笀鏄熺骇鑼冨洿鏄?.0鍒?.0")]
    public decimal StarLevel { get; set; } = 3.0m;

    [RegularExpression("^[YN]$", ErrorMessage = "鏄惁鍐呴儴璁插笀鍙兘鏄痀鎴朜")]
    public string IsInternal { get; set; } = "Y";
}
