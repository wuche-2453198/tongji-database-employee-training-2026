using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Dtos.Ratings;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations;
public sealed class RatingService : IRatingService
{
    private readonly IRatingRepository _ratingRepository;

    public RatingService(IRatingRepository ratingRepository)
    {
        _ratingRepository = ratingRepository;
    }

    public async Task<bool> CreateRatingAsync(CreateRatingRequest request, int employeeId)
    {
        if (request.Score < 1 || request.Score > 5)
            throw new BadRequestException("评分必须在1到5分之间");

        var exists = await _ratingRepository.ExistsByEmployeeAndCourseAsync(employeeId, request.CourseId);
        if (exists)
            throw new ConflictException("您已经对该课程进行过评分，不能重复评价");

        return await _ratingRepository.CreateAsync(request, employeeId);
    }

    public async Task<IEnumerable<TrainerRating>> GetRatingListAsync(int? courseId, int? trainerId, int page, int pageSize)
    {
        return await _ratingRepository.GetListAsync(courseId, trainerId, page, pageSize);
    }

    public async Task<bool> VerifyRatingAsync(int ratingId, string verifyComment, int hrVerifierEmpId)
    {
        var rating = await _ratingRepository.GetByIdAsync(ratingId);
        if (rating == null)
            throw new NotFoundException("评分记录不存在");

        if (rating.HrVerified == "Y")
            throw new ConflictException("该评分已经复核过了");

        return await _ratingRepository.VerifyAsync(ratingId, verifyComment, hrVerifierEmpId);
    }
}
