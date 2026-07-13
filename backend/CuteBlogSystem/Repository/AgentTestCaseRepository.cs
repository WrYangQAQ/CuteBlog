using CuteBlogSystem.Config;
using CuteBlogSystem.DTO;
using CuteBlogSystem.DTO.Agent;
using CuteBlogSystem.Entity;
using CuteBlogSystem.Enum;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace CuteBlogSystem.Repository
{
    public class AgentTestCaseRepository
    {
        private readonly MyDbContext _dbContext;

        public AgentTestCaseRepository(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 从数据库中查出所有的测试用例
        public async Task<List<AgentTestCase>> GetAllCaseAsync()
        {
            var cases = await _dbContext.AgentTestCases
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Id)
                .ToListAsync();
            return cases;
        }

        // 从数据库中查出所有启用的测试用例
        public async Task<List<AgentTestCase>> GetEnabledCaseAsync()
        {
            var cases = await _dbContext.AgentTestCases
                .Where(c => c.IsEnabled && !c.IsDeleted)
                .OrderBy(c => c.Id)
                .ToListAsync();
            return cases;
        }

        // 从数据库中查出所有禁用的测试用例
        public async Task<List<AgentTestCase>> GetDisabledCaseAsync()
        {
            var cases = await _dbContext.AgentTestCases
                .Where(c => !c.IsEnabled && !c.IsDeleted)
                .OrderBy(c => c.Id)
                .ToListAsync();
            return cases;
        }

        // 根据ID启用某条测试用例
        public async Task<ApiResponse> EnableCaseAsync(int caseId)
        {
            var testCase = await _dbContext.AgentTestCases.FindAsync(caseId);

            if (testCase == null) 
            {
                return new ApiResponse(false, "该条记录不存在！", code: ResponseCode.NotFound);
            }

            if (testCase.IsDeleted)
            {
                return new ApiResponse(false, "该条记录已被删除，不可再访问！", code: ResponseCode.BadRequest);
            }

            if (testCase.IsEnabled)
            {
                return new ApiResponse(false, "该条记录已经是启用状态！", code: ResponseCode.BadRequest);
            }

            testCase.IsEnabled = true;
            var affectedRows = await _dbContext.SaveChangesAsync();

            return affectedRows > 0
                ? new ApiResponse(true, $"成功启用用例 ID={caseId}", code: ResponseCode.Success)
                : new ApiResponse(false, "保存失败，未影响任何行", code: ResponseCode.InternalError);
        }

        // 根据ID禁用某条测试用例
        public async Task<ApiResponse> DisableCaseAsync(int caseId)
        {
            var testCase = await _dbContext.AgentTestCases.FindAsync(caseId);

            if (testCase == null)
            {
                return new ApiResponse(false, "该条记录不存在！", code: ResponseCode.NotFound);
            }

            if (testCase.IsDeleted)
            {
                return new ApiResponse(false, "该条记录已被删除，不可再访问！", code: ResponseCode.BadRequest);
            }

            if (!testCase.IsEnabled)
            {
                return new ApiResponse(false, "该条记录已经是禁用状态！", code: ResponseCode.BadRequest);
            }

            testCase.IsEnabled = false;
            var affectedRows = await _dbContext.SaveChangesAsync();

            return affectedRows > 0
                ? new ApiResponse(true, $"成功禁用用例 ID={caseId}", code: ResponseCode.Success)
                : new ApiResponse(false, "保存失败，未影响任何行", code: ResponseCode.InternalError);
        }

        // 添加测试用例
        public async Task<bool> AddTestCaseAsync(AgentTestCase testCase)
        {
            await _dbContext.AgentTestCases.AddAsync(testCase);
            var affectedRows = await _dbContext.SaveChangesAsync();
            return (affectedRows > 0);
        }

        // 根据 Id 查询单条评估测试用例（不管启用或禁用状态）
        public async Task<AgentTestCase?> FindByIdAsync(int caseId)
        {
            var testCase = await _dbContext.AgentTestCases.FindAsync(caseId);
            if (testCase is null)
            {
                return null;
            }
            else if (testCase.IsDeleted)
            {
                return null;
            }
            return testCase;
        }

        // 根据 Id 列表批量查询评估测试用例（只返回已被启用的测试案例）
        public async Task<List<AgentTestCase>?> FindByIdAsync(List<int> caseIds)
        {
            List<AgentTestCase> testCases;

            // 处理 null 列表，直接返回空列表（避免生成无效 SQL）
            if (caseIds == null)
            {
                return new List<AgentTestCase>();  // 返回空列表而不是 null，避免上层判空麻烦
            }
            else if (caseIds.Count == 0)
            {
                testCases = await GetEnabledCaseAsync();  // 传入数量为空的列表用于查询全部启用列表
            }
            else
            {
                // 使用 Contains 生成 SQL IN 查询，一次性查出所有匹配的用例
                testCases = await _dbContext.AgentTestCases
                    .Where(testCase => caseIds.Contains(testCase.Id) && testCase.IsEnabled && !testCase.IsDeleted)
                    .ToListAsync();
            }

            return testCases;  // 如果没有匹配，返回空列表
        }

        // 根据 Dto 传入数据更新对应的 TestCase
        public async Task<AgentTestCase?> UpdateTestCaseAsync(
            AgentTestCaseUpdateDto caseDto,
            string expectedActionJson,
            string expectedContainsJson)
        {
            var updatedCase = await _dbContext.AgentTestCases
                .FirstOrDefaultAsync(c => c.Id == caseDto.Id && !c.IsDeleted);

            if (updatedCase == null)
            {
                return null;
            }

            // 更新所有字段
            updatedCase.UpdatedAt = DateTime.UtcNow;
            updatedCase.CaseName = caseDto.CaseName;
            updatedCase.UserMessage = caseDto.UserMessage;

            if (!string.IsNullOrWhiteSpace(caseDto.SessionId))
            {
                updatedCase.SessionId = caseDto.SessionId;
            }

            updatedCase.ExpectedActionsJson = expectedActionJson;
            updatedCase.ExpectedAnswerContainsJson = expectedContainsJson;
            updatedCase.ExpectedSuccess = caseDto.ExpectSuccess;
            updatedCase.ExpectedRequiresConfirmation = caseDto.ExpectRequiresConfirmation;
            updatedCase.ExpectedAnswerSummary = caseDto.ExpectedAnswerSummary;
            updatedCase.EnableSemanticJudge = caseDto.EnableSemanticJudge;
            updatedCase.SemanticJudgeThreshold = caseDto.SemanticJudgeThreshold;
            updatedCase.Category = caseDto.Category;
            updatedCase.Remark = caseDto.Remark;

            await _dbContext.SaveChangesAsync();

            return updatedCase;
        }

        // 根据传入 CaseId 对相应评估测试用例做删除
        public async Task<ApiResponse> DeleteCaseAsync(int caseId)
        {
            var testCase = await _dbContext.AgentTestCases
                .FirstOrDefaultAsync(c => c.Id == caseId && !c.IsDeleted);

            if (testCase == null)
            {
                return new ApiResponse(false, "该条记录不存在或已被删除！", code: ResponseCode.NotFound);
            }

            testCase.IsDeleted = true;
            testCase.IsEnabled = false;
            testCase.UpdatedAt = DateTime.UtcNow;

            var affectedRows = await _dbContext.SaveChangesAsync();

            return affectedRows > 0
                ? new ApiResponse(true, $"成功删除用例 ID={caseId}", code: ResponseCode.Success)
                : new ApiResponse(false, "删除失败，未影响任何行", code: ResponseCode.InternalError);
        }
    }
}
