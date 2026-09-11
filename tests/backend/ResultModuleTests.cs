using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Dtos.Certificates;
using TrainingManagement.Api.Dtos.Ratings;
using TrainingManagement.Api.Dtos.Tests;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.TrainingRequest;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Services.Implementations;

namespace TrainingManagement.Api.ModuleTests;

/// <summary>
/// 成果评估模块规格场景:
/// 未完成发证、缺少必需 POST 成绩、重复证书、越权查询,以及 PRE/POST 录入时点(D-012)。
/// </summary>
internal static class ResultModuleTests
{
    private const long CompletedCourseId = 27;
    private const long PostTestCourseId = 101;
    private const long StartedCourseId = 102;
    private const long UnfinishedCourseId = 103;

    public static IEnumerable<(string Name, Func<Task> Run)> GetTests()
    {
        yield return ("Certificate rejects registration that is not COMPLETED", CertificateRejectsUncompletedAsync);
        yield return ("Certificate requires POST score when course configures post test", CertificateRequiresPostScoreAsync);
        yield return ("Certificate requires POST score at least 60", CertificateRequiresPostScore60Async);
        yield return ("Certificate rejects duplicates", CertificateRejectsDuplicateAsync);
        yield return ("Certificate detail blocks other employees", CertificateBlocksOtherEmployeeAsync);
        yield return ("Certificate detail allows HR and owner", CertificateAllowsHrAndOwnerAsync);
        yield return ("Certificate status maps expiry to VALID/EXPIRING/EXPIRED", CertificateStatusMappingAsync);
        yield return ("PRE test requires a valid registration", PreTestRequiresValidRegistrationAsync);
        yield return ("PRE test rejects after course start", PreTestRejectsAfterStartAsync);
        yield return ("POST test requires completed registration", PostTestRequiresCompletedRegistrationAsync);
        yield return ("Duplicate PRE/POST score is rejected as conflict", DuplicateTestRejectedAsync);
        yield return ("Out-of-range score is rejected", InvalidScoreRejectedAsync);
        yield return ("Non PRE/POST test type is rejected", InvalidTypeRejectedAsync);
        yield return ("Future test time is rejected", FutureTestedAtRejectedAsync);
        yield return ("Unknown course is rejected as not found", CourseNotFoundRejectedAsync);
        yield return ("Rating requires completed training", RatingRequiresCompletedAsync);
        yield return ("Rating rejects duplicate by employee and course", RatingRejectsDuplicateAsync);
        yield return ("Improvement reports change and 0-safe rate", ImprovementReportsScoresAsync);
    }

    // ---------- 证书 ----------

    private static async Task CertificateRejectsUncompletedAsync()
    {
        var repository = new FakeCertificateRepository();
        repository.Registrations[5] = new Registration { RegId = 5, EmpId = 55, CourseId = (int)UnfinishedCourseId, Status = "REGISTERED" };
        var service = new CertificateService(repository);

        var exception = await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.GenerateCertificateAsync(new GenerateCertificateRequest { RegistrationId = 5 }, IssuedBy),
            "未完成培训不应发证");
        TestAssert.True(exception.Message.Contains("尚未完成培训"), "应提示尚未完成培训");
    }

    private static async Task CertificateRequiresPostScoreAsync()
    {
        var repository = CreateRepoWithCompletedRegistration();
        repository.CourseGate = new ResultCourseGate { Exists = true, PostTestUrl = "https://test.example/post" };
        repository.PostScore = null;
        var service = new CertificateService(repository);

        var exception = await TestAssert.ThrowsAsync<BusinessException>(
            () => service.GenerateCertificateAsync(new GenerateCertificateRequest { RegistrationId = 1 }, IssuedBy),
            "缺 POST 成绩不应发证");
        TestAssert.True(exception.Message.Contains("POST"), "应提示缺 POST 成绩");
    }

    private static async Task CertificateRequiresPostScore60Async()
    {
        var repository = CreateRepoWithCompletedRegistration();
        repository.CourseGate = new ResultCourseGate { Exists = true, PostTestUrl = "https://test.example/post" };
        repository.PostScore = 59m;
        var service = new CertificateService(repository);

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.GenerateCertificateAsync(new GenerateCertificateRequest { RegistrationId = 1 }, IssuedBy),
            "POST 不足 60 分不应发证");

        repository.PostScore = 60m;
        var result = await service.GenerateCertificateAsync(new GenerateCertificateRequest { RegistrationId = 1 }, IssuedBy);
        TestAssert.True(result.CertificateId > 0, "POST 恰好 60 分应发证");
    }

    private static async Task CertificateRejectsDuplicateAsync()
    {
        var repository = CreateRepoWithCompletedRegistration();
        repository.Certificates.Add(new TrainingCertificate { CertId = 9, EmpId = 55, CourseId = (int)CompletedCourseId });
        var service = new CertificateService(repository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.GenerateCertificateAsync(new GenerateCertificateRequest { RegistrationId = 1 }, IssuedBy),
            "重复发证应拒绝");
    }

    private static async Task CertificateBlocksOtherEmployeeAsync()
    {
        var repository = CreateRepoWithCompletedRegistration();
        repository.Certificates.Add(new TrainingCertificate { CertId = 9, EmpId = 58, CourseId = (int)CompletedCourseId });
        var service = new CertificateService(repository);

        var actor = new ActorContext(55, new[] { "EMPLOYEE" });
        await TestAssert.ThrowsAsync<ForbiddenApiException>(
            () => service.GetCertificateByIdAsync(actor, 9),
            "员工查看他人证书应 403");
    }

    private static async Task CertificateAllowsHrAndOwnerAsync()
    {
        var repository = CreateRepoWithCompletedRegistration();
        repository.Certificates.Add(new TrainingCertificate { CertId = 9, EmpId = 55, CourseId = (int)CompletedCourseId });
        var service = new CertificateService(repository);

        var owner = await service.GetCertificateByIdAsync(new ActorContext(55, new[] { "EMPLOYEE" }), 9);
        TestAssert.Equal(9, owner.CertId, "本人可查");

        var hr = await service.GetCertificateByIdAsync(new ActorContext(57, new[] { "HR" }), 9);
        TestAssert.Equal(9, hr.CertId, "HR 可查");

        var admin = await service.GetCertificateByIdAsync(new ActorContext(54, new[] { "ADMIN" }), 9);
        TestAssert.Equal(9, admin.CertId, "管理员可查");
    }

    private static Task CertificateStatusMappingAsync()
    {
        var today = DateTime.Today;
        TestAssert.Equal(
            "VALID",
            new TrainingCertificate { ExpireDate = null }.Status,
            "EXPIRE_DATE 为空应按长期有效处理(修复“未知状态”)");
        TestAssert.Equal(
            "EXPIRED",
            new TrainingCertificate { ExpireDate = today.AddDays(-1) }.Status,
            "已过期应为 EXPIRED");
        TestAssert.Equal(
            "EXPIRING",
            new TrainingCertificate { ExpireDate = today.AddDays(15) }.Status,
            "30 天内到期应为 EXPIRING");
        TestAssert.Equal(
            "VALID",
            new TrainingCertificate { ExpireDate = today.AddDays(90) }.Status,
            "远期到期应为 VALID");
        return Task.CompletedTask;
    }

    // ---------- 成绩 ----------

    private static async Task PreTestRequiresValidRegistrationAsync()
    {
        var repository = new FakeTestRepository
        {
            CourseGate = new ResultCourseGate { Exists = true, StartAt = DateTime.Now.AddDays(7), EndAt = DateTime.Now.AddDays(8) },
            RegistrationStatus = null,
        };
        var service = new TestService(repository);

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.CreateTestAsync(new CreateTestRequest { EmpId = 55, CourseId = (int)CompletedCourseId, TestType = "PRE", Score = 70 }, 57),
            "无有效报名不能录 PRE");

        repository.RegistrationStatus = "REGISTERED";
        var created = await service.CreateTestAsync(
            new CreateTestRequest { EmpId = 55, CourseId = (int)CompletedCourseId, TestType = "PRE", Score = 70 }, 57);
        TestAssert.True(created, "有效报名应可录 PRE");
    }

    private static async Task PreTestRejectsAfterStartAsync()
    {
        var repository = new FakeTestRepository
        {
            CourseGate = new ResultCourseGate { Exists = true, StartAt = DateTime.Now.AddHours(-1), EndAt = DateTime.Now.AddDays(1) },
            RegistrationStatus = "SIGNED_IN",
        };
        var service = new TestService(repository);

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.CreateTestAsync(new CreateTestRequest { EmpId = 55, CourseId = (int)StartedCourseId, TestType = "PRE", Score = 70 }, 57),
            "开课后不能录 PRE");
    }

    private static async Task PostTestRequiresCompletedRegistrationAsync()
    {
        var repository = new FakeTestRepository
        {
            CourseGate = new ResultCourseGate { Exists = true, StartAt = DateTime.Now.AddDays(-1), EndAt = DateTime.Now.AddDays(1) },
            RegistrationStatus = "SIGNED_IN",
        };
        var service = new TestService(repository);

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.CreateTestAsync(new CreateTestRequest { EmpId = 55, CourseId = (int)StartedCourseId, TestType = "POST", Score = 90 }, 57),
            "未完成培训不能录 POST");

        repository.RegistrationStatus = "COMPLETED";
        var created = await service.CreateTestAsync(
            new CreateTestRequest { EmpId = 55, CourseId = (int)StartedCourseId, TestType = "POST", Score = 90 }, 57);
        TestAssert.True(created, "完成培训后应可录 POST");
    }

    private static async Task DuplicateTestRejectedAsync()
    {
        var repository = new FakeTestRepository
        {
            CourseGate = new ResultCourseGate { Exists = true, StartAt = DateTime.Now.AddDays(7), EndAt = DateTime.Now.AddDays(8) },
            RegistrationStatus = "REGISTERED",
        };
        repository.Scores[(55, (int)CompletedCourseId)] = (70m, null);
        var service = new TestService(repository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.CreateTestAsync(new CreateTestRequest { EmpId = 55, CourseId = (int)CompletedCourseId, TestType = "PRE", Score = 80 }, 57),
            "同一员工+课程+类型重复录入应 409");
    }

    private static async Task InvalidScoreRejectedAsync()
    {
        var repository = new FakeTestRepository
        {
            CourseGate = new ResultCourseGate { Exists = true, StartAt = DateTime.Now.AddDays(7) },
            RegistrationStatus = "REGISTERED",
        };
        var service = new TestService(repository);

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.CreateTestAsync(new CreateTestRequest { EmpId = 55, CourseId = (int)CompletedCourseId, TestType = "PRE", Score = -1 }, 57),
            "负分应拒绝");
        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.CreateTestAsync(new CreateTestRequest { EmpId = 55, CourseId = (int)CompletedCourseId, TestType = "PRE", Score = 101 }, 57),
            "超过 100 分应拒绝");
    }

    private static async Task InvalidTypeRejectedAsync()
    {
        var service = new TestService(new FakeTestRepository());

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.CreateTestAsync(new CreateTestRequest { EmpId = 55, CourseId = (int)CompletedCourseId, TestType = "MID", Score = 70 }, 57),
            "类型非 PRE/POST 应拒绝");
    }

    private static async Task FutureTestedAtRejectedAsync()
    {
        var service = new TestService(new FakeTestRepository());

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.CreateTestAsync(
                new CreateTestRequest { EmpId = 55, CourseId = (int)CompletedCourseId, TestType = "PRE", Score = 70, TestedAt = DateTime.Now.AddDays(1) }, 57),
            "测试时间晚于当前应拒绝");
    }

    private static async Task CourseNotFoundRejectedAsync()
    {
        var repository = new FakeTestRepository { CourseGate = new ResultCourseGate { Exists = false } };
        var service = new TestService(repository);

        await TestAssert.ThrowsAsync<NotFoundApiException>(
            () => service.CreateTestAsync(new CreateTestRequest { EmpId = 55, CourseId = 9999, TestType = "PRE", Score = 70 }, 57),
            "课程不存在应 404");
    }

    private static async Task ImprovementReportsScoresAsync()
    {
        var repository = new FakeTestRepository();
        repository.Scores[(55, 30)] = (65m, null);
        var service = new TestService(repository);

        var partial = await service.GetImprovementAsync(55, 30);
        TestAssert.True(partial.Improvement is null, "缺 POST 时提升值应为 null");
        TestAssert.True(partial.ImprovementRate is null, "缺 POST 时提升率应为 null");

        repository.Scores[(55, 30)] = (80m, 100m);
        var full = await service.GetImprovementAsync(55, 30);
        TestAssert.Equal(20m, full.Improvement, "提升值应为 POST - PRE");
        TestAssert.Equal(25m, full.ImprovementRate, "80 -> 100 提升率应为 25%");

        repository.Scores[(55, 31)] = (0m, 50m);
        var zeroPre = await service.GetImprovementAsync(55, 31);
        TestAssert.Equal(50m, zeroPre.Improvement, "PRE 为 0 分时提升值仍应计算");
        TestAssert.True(zeroPre.ImprovementRate is null, "PRE 为 0 分时提升率不可计算应为 null");
    }

    // ---------- 评分 ----------

    private static async Task RatingRequiresCompletedAsync()
    {
        var repository = new FakeRatingRepository { HasCompleted = false };
        var service = new RatingService(repository);

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.CreateRatingAsync(new CreateRatingRequest { CourseId = (int)UnfinishedCourseId, TrainerId = 1, Score = 5 }, 55),
            "未完成培训不能评分");
    }

    private static async Task RatingRejectsDuplicateAsync()
    {
        var repository = new FakeRatingRepository { HasCompleted = true, ExistsRating = true };
        var service = new RatingService(repository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.CreateRatingAsync(new CreateRatingRequest { CourseId = (int)CompletedCourseId, TrainerId = 1, Score = 5 }, 55),
            "重复评分应拒绝");
    }

    // ---------- 夹具 ----------

    private const int IssuedBy = 57;

    private static FakeCertificateRepository CreateRepoWithCompletedRegistration()
    {
        var repository = new FakeCertificateRepository();
        repository.Registrations[1] = new Registration { RegId = 1, EmpId = 55, CourseId = (int)CompletedCourseId, Status = "COMPLETED" };
        repository.CourseGate = new ResultCourseGate { Exists = true, PostTestUrl = "https://test.example/post" };
        repository.PostScore = 88m;
        return repository;
    }

    private sealed class FakeCertificateRepository : Repositories.Interfaces.ICertificateRepository
    {
        public Dictionary<int, Registration> Registrations { get; } = new();

        public List<TrainingCertificate> Certificates { get; } = new();

        public ResultCourseGate CourseGate { get; set; } = new() { Exists = true };

        public decimal? PostScore { get; set; }

        public int NextCertId { get; set; } = 100;

        public Task<Registration?> GetRegistrationByIdAsync(int registrationId)
        {
            Registrations.TryGetValue(registrationId, out var registration);
            return Task.FromResult(registration);
        }

        public Task<bool> ExistsByEmployeeAndCourseAsync(int employeeId, int courseId)
        {
            return Task.FromResult(Certificates.Any(c => c.EmpId == employeeId && c.CourseId == courseId));
        }

        public Task<int> CreateAsync(string certificateNo, int employeeId, int courseId, int issuedByEmpId)
        {
            var certId = ++NextCertId;
            Certificates.Add(new TrainingCertificate
            {
                CertId = certId,
                CertCode = certificateNo,
                EmpId = employeeId,
                CourseId = courseId,
                IssuedByEmpId = issuedByEmpId,
                Notified = "N",
            });
            return Task.FromResult(certId);
        }

        public Task<IEnumerable<TrainingCertificate>> GetByEmployeeIdAsync(int employeeId)
        {
            return Task.FromResult<IEnumerable<TrainingCertificate>>(
                Certificates.Where(c => c.EmpId == employeeId).ToArray());
        }

        public Task<TrainingCertificate?> GetByIdAsync(int id)
        {
            return Task.FromResult(Certificates.FirstOrDefault(c => c.CertId == id));
        }

        public Task<bool> UpdateNotifyFlagAsync(int id)
        {
            var certificate = Certificates.FirstOrDefault(c => c.CertId == id);
            if (certificate is null) return Task.FromResult(false);
            certificate.Notified = "Y";
            certificate.NotifiedAt = DateTime.Now;
            return Task.FromResult(true);
        }

        public Task<ResultCourseGate> GetCourseGateAsync(int courseId)
        {
            return Task.FromResult(CourseGate);
        }

        public Task<decimal?> GetPostTestScoreAsync(int employeeId, int courseId)
        {
            return Task.FromResult(PostScore);
        }

        public Task<(IReadOnlyList<TrainingCertificate> Items, int Total)> GetPagedListAsync(
            string? employeeName,
            string? courseName,
            DateTime? startDateFrom,
            DateTime? startDateTo,
            int page,
            int pageSize)
        {
            IReadOnlyList<TrainingCertificate> items = Certificates;
            return Task.FromResult((items, items.Count));
        }

        public Task<(IReadOnlyList<CertificateCandidate> Items, int Total)> GetCandidatesAsync(
            string? employeeName,
            string? courseName,
            int page,
            int pageSize)
        {
            IReadOnlyList<CertificateCandidate> items = Array.Empty<CertificateCandidate>();
            return Task.FromResult((items, 0));
        }
    }

    private sealed class FakeTestRepository : Repositories.Interfaces.ITestRepository
    {
        public ResultCourseGate CourseGate { get; set; } = new() { Exists = true };

        /// <summary>该员工该课程的报名状态;null 表示没有报名记录。</summary>
        public string? RegistrationStatus { get; set; }

        public Dictionary<(int EmpId, int CourseId), (decimal? Pre, decimal? Post)> Scores { get; } = new();

        public Task<bool> CreateAsync(CreateTestRequest request, int recordedByEmpId, DateTime testedAt)
        {
            Scores.TryGetValue((request.EmpId, request.CourseId), out var current);
            var score = request.TestType == "PRE" ? (request.Score, current.Post) : (current.Pre, (decimal?)request.Score);
            Scores[(request.EmpId, request.CourseId)] = score;
            return Task.FromResult(true);
        }

        public Task<bool> ExistsByEmployeeCourseAndTypeAsync(int employeeId, int courseId, string testType)
        {
            Scores.TryGetValue((employeeId, courseId), out var current);
            var exists = testType == "PRE" ? current.Pre is not null : current.Post is not null;
            return Task.FromResult(exists);
        }

        public Task<PagedResult<TrainingTest>> GetPagedListAsync(
            int? employeeId,
            int? courseId,
            string? testType,
            string? employeeName,
            string? courseName,
            DateTime? startDateFrom,
            DateTime? startDateTo,
            int page,
            int pageSize)
        {
            return Task.FromResult(new PagedResult<TrainingTest>(Array.Empty<TrainingTest>(), page, pageSize, 0));
        }

        public Task<PagedResult<TestScoreSummary>> GetScoreSummariesAsync(
            int? employeeId,
            int? courseId,
            string? employeeName,
            string? courseName,
            DateTime? startDateFrom,
            DateTime? startDateTo,
            int page,
            int pageSize)
        {
            var items = Scores
                .Where(entry => !employeeId.HasValue || entry.Key.EmpId == employeeId.Value)
                .Where(entry => !courseId.HasValue || entry.Key.CourseId == courseId.Value)
                .Select(entry => new TestScoreSummary
                {
                    EmpId = entry.Key.EmpId,
                    CourseId = entry.Key.CourseId,
                    PreScore = entry.Value.Pre,
                    PostScore = entry.Value.Post,
                    Change = entry.Value.Pre.HasValue && entry.Value.Post.HasValue
                        ? entry.Value.Post - entry.Value.Pre
                        : null
                })
                .ToArray();
            return Task.FromResult(new PagedResult<TestScoreSummary>(items, page, pageSize, items.Length));
        }

        public Task<ResultCourseGate> GetCourseGateAsync(int courseId)
        {
            return Task.FromResult(CourseGate);
        }

        public Task<string?> GetRegistrationStatusAsync(int employeeId, int courseId)
        {
            return Task.FromResult(RegistrationStatus);
        }

        public Task<(decimal? PreScore, decimal? PostScore)> GetScoresAsync(int employeeId, int courseId)
        {
            Scores.TryGetValue((employeeId, courseId), out var current);
            return Task.FromResult(current);
        }
    }

    private sealed class FakeRatingRepository : Repositories.Interfaces.IRatingRepository
    {
        public bool HasCompleted { get; set; }

        public bool ExistsRating { get; set; }

        public List<TrainerRating> Ratings { get; } = new();

        public Task<bool> CreateAsync(Dtos.Ratings.CreateRatingRequest request, int employeeId)
        {
            Ratings.Add(new TrainerRating { RatingId = Ratings.Count + 1, EmpId = employeeId, CourseId = request.CourseId });
            return Task.FromResult(true);
        }

        public Task<(IReadOnlyList<TrainerRating> Items, int Total)> GetListAsync(
            int? courseId, int? trainerId, int page, int pageSize)
        {
            IReadOnlyList<TrainerRating> items = Ratings;
            return Task.FromResult((items, Ratings.Count));
        }

        public Task<(IReadOnlyList<TrainerRating> Items, int Total)> GetMyListAsync(
            int employeeId,
            string? keyword,
            DateTime? startDateFrom,
            DateTime? startDateTo,
            int page,
            int pageSize)
        {
            IReadOnlyList<TrainerRating> items = Ratings
                .Where(rating => rating.EmpId == employeeId)
                .ToArray();

            return Task.FromResult((items, items.Count));
        }

        public Task<bool> ExistsByEmployeeAndCourseAsync(int employeeId, int courseId)
        {
            return Task.FromResult(ExistsRating);
        }

        public Task<TrainerRating?> GetByIdAsync(int ratingId)
        {
            return Task.FromResult(Ratings.FirstOrDefault(r => r.RatingId == ratingId));
        }

        public Task<bool> VerifyAsync(int ratingId, string verifyComment, int hrVerifierEmpId)
        {
            var rating = Ratings.FirstOrDefault(r => r.RatingId == ratingId);
            if (rating is null) return Task.FromResult(false);
            rating.HrVerified = "Y";
            return Task.FromResult(true);
        }

        public Task<bool> HasCompletedRegistrationAsync(int employeeId, int courseId)
        {
            return Task.FromResult(HasCompleted);
        }

        public Task<(decimal? AverageScore, int RatingCount)> GetAverageAsync(int courseId, int? trainerId)
        {
            var scores = Ratings.Where(r => r.CourseId == courseId).Select(r => (decimal)r.Score).ToArray();
            if (scores.Length == 0) return Task.FromResult(((decimal?)null, 0));
            return Task.FromResult(((decimal?)scores.Average(), scores.Length));
        }
    }
}
