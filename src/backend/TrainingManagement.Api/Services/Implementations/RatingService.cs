using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Ratings;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations;
public sealed class RatingService : IRatingService
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    private readonly IRatingRepository _ratingRepository;

    public RatingService(IRatingRepository ratingRepository)
    {
        _ratingRepository = ratingRepository;
    }

    public async Task<bool> CreateRatingAsync(CreateRatingRequest request, int employeeId)
    {
        if (request.Score < 1 || request.Score > 5)
            throw new BusinessException("评分必须在1到5分之间");

        // RES-01:仅已完成培训的课程可以评分。
        if (!await _ratingRepository.HasCompletedRegistrationAsync(employeeId, request.CourseId))
            throw new BusinessException("仅已完成培训的课程可以评分");

        var exists = await _ratingRepository.ExistsByEmployeeAndCourseAsync(employeeId, request.CourseId);
        if (exists)
            throw new ConflictApiException("您已经对该课程进行过评分，不能重复评价");

        return await _ratingRepository.CreateAsync(request, employeeId);
    }

    public async Task<PagedResult<TrainerRating>> GetRatingListAsync(
        int? courseId, int? trainerId, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var (items, total) = await _ratingRepository.GetListAsync(courseId, trainerId, page, pageSize);
        return new PagedResult<TrainerRating>(items, page, pageSize, total);
    }

    public async Task<PagedResult<TrainerRating>> GetMyRatingListAsync(
        int employeeId, string? keyword, DateTime? startDateFrom, DateTime? startDateTo, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var (items, total) = await _ratingRepository.GetMyListAsync(
            employeeId, keyword, startDateFrom, startDateTo, page, pageSize);
        return new PagedResult<TrainerRating>(items, page, pageSize, total);
    }

    public async Task<RatingAverageResponse> GetAverageAsync(int courseId, int? trainerId)
    {
        var (average, count) = await _ratingRepository.GetAverageAsync(courseId, trainerId);
        return new RatingAverageResponse { AverageScore = average, RatingCount = count };
    }

    public async Task<bool> VerifyRatingAsync(int ratingId, string verifyComment, int hrVerifierEmpId)
    {
        var rating = await _ratingRepository.GetByIdAsync(ratingId);
        if (rating == null)
            throw new NotFoundApiException("评分记录不存在");

        if (rating.HrVerified == "Y")
            throw new ConflictApiException("该评分已经复核过了");

        return await _ratingRepository.VerifyAsync(ratingId, verifyComment, hrVerifierEmpId);
    }
}
