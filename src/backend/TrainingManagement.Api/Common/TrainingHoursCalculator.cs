namespace TrainingManagement.Api.Common;

/// <summary>
/// 统一迟到扣减算法：
/// 迟到分钟数 = 签到时间与课程开始时间的整分钟差（不足一分钟不计）；
/// 扣减学时 = 课程学时 × 迟到分钟 / 60，四舍五入到 0.5 小时，上限为课程学时；
/// 实际学时 = 课程学时 - 扣减学时，最小为 0。
/// </summary>
public static class TrainingHoursCalculator
{
    public static (int LatenessMinutes, decimal DeductHours, decimal ActualHours) Calculate(
        DateTime signInAt,
        DateTime courseStartAt,
        decimal durationHours)
    {
        var latenessMinutes = (int)Math.Max(
            0,
            Math.Floor((signInAt - courseStartAt).TotalMinutes));

        var rawDeduct = durationHours * latenessMinutes / 60m;
        var roundedDeduct = Math.Round(
            rawDeduct * 2m,
            MidpointRounding.AwayFromZero) / 2m;
        var deductHours = Math.Min(durationHours, roundedDeduct);
        var actualHours = Math.Max(0m, durationHours - deductHours);

        return (latenessMinutes, deductHours, actualHours);
    }
}
