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

    [Range(1, int.MaxValue, ErrorMessage = "页码必须大于0")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "每页数量必须在1到100之间")]
    public int PageSize { get; set; } = 20;
}
