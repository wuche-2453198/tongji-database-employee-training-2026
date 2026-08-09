using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Api.Dtos.Trainer;

public sealed class TrainerQuery
{
    [MaxLength(50)]
    public string? TrainerName { get; set; }

    [MaxLength(100)]
    public string? Company { get; set; }

    [RegularExpression(
        "^[YN]$",
        ErrorMessage = "是否内部讲师只能是Y或N")]
    public string? IsInternal { get; set; }
}
