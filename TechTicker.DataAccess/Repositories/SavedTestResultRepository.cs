using Microsoft.EntityFrameworkCore;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for managing saved test results
/// </summary>
public class SavedTestResultRepository : ISavedTestResultRepository
{
    private readonly TechTickerDbContext _context;

    public SavedTestResultRepository(TechTickerDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<SavedTestResult> Results, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        List<string>? tags = null,
        Guid? createdBy = null,
        bool? success = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _context.SavedTestResults
            .Include(r => r.Tags)
            .Include(r => r.CreatedByUser)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            query = query.Where(r => 
                r.Name.ToLower().Contains(lowerSearchTerm) ||
                (r.Description != null && r.Description.ToLower().Contains(lowerSearchTerm)) ||
                r.TestUrl.ToLower().Contains(lowerSearchTerm));
        }

        if (tags != null && tags.Any())
        {
            query = query.Where(r => r.Tags.Any(t => tags.Contains(t.Tag)));
        }

        if (createdBy.HasValue)
        {
            query = query.Where(r => r.CreatedBy == createdBy.Value);
        }

        if (success.HasValue)
        {
            query = query.Where(r => r.Success == success.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.ExecutedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.ExecutedAt <= toDate.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination and ordering
        var results = await query
            .OrderByDescending(r => r.SavedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (results, totalCount);
    }

    public async Task<SavedTestResult?> GetByIdAsync(Guid id)
    {
        return await _context.SavedTestResults
            .Include(r => r.Tags)
            .Include(r => r.CreatedByUser)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<SavedTestResult>> GetByProfileHashAsync(string profileHash)
    {
        return await _context.SavedTestResults
            .Include(r => r.Tags)
            .Include(r => r.CreatedByUser)
            .Where(r => r.ProfileHash == profileHash)
            .OrderByDescending(r => r.SavedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SavedTestResult>> GetByTestUrlAsync(string testUrl)
    {
        return await _context.SavedTestResults
            .Include(r => r.Tags)
            .Include(r => r.CreatedByUser)
            .Where(r => r.TestUrl == testUrl)
            .OrderByDescending(r => r.SavedAt)
            .ToListAsync();
    }

    public async Task<SavedTestResult> CreateAsync(SavedTestResult savedTestResult)
    {
        savedTestResult.Id = Guid.NewGuid();
        savedTestResult.SavedAt = DateTime.UtcNow;

        _context.SavedTestResults.Add(savedTestResult);
        await _context.SaveChangesAsync();
        
        // Reload with includes
        return await GetByIdAsync(savedTestResult.Id) ?? savedTestResult;
    }

    public async Task<SavedTestResult> UpdateAsync(SavedTestResult savedTestResult)
    {
        var existing = await _context.SavedTestResults
            .Include(r => r.Tags)
            .FirstOrDefaultAsync(r => r.Id == savedTestResult.Id);
        
        if (existing == null)
        {
            throw new InvalidOperationException($"Saved test result with ID {savedTestResult.Id} not found");
        }

        // Update properties
        existing.Name = savedTestResult.Name;
        existing.Description = savedTestResult.Description;
        existing.TestUrl = savedTestResult.TestUrl;
        existing.Success = savedTestResult.Success;
        existing.ExecutedAt = savedTestResult.ExecutedAt;
        existing.Duration = savedTestResult.Duration;
        existing.ActionsExecuted = savedTestResult.ActionsExecuted;
        existing.ErrorCount = savedTestResult.ErrorCount;
        existing.ProfileHash = savedTestResult.ProfileHash;
        existing.TestResultJson = savedTestResult.TestResultJson;
        existing.ProfileJson = savedTestResult.ProfileJson;
        existing.OptionsJson = savedTestResult.OptionsJson;
        existing.MetadataJson = savedTestResult.MetadataJson;
        existing.ScreenshotsData = savedTestResult.ScreenshotsData;
        existing.VideoRecording = savedTestResult.VideoRecording;

        // Update tags
        _context.SavedTestResultTags.RemoveRange(existing.Tags);
        if (savedTestResult.Tags != null)
        {
            existing.Tags = savedTestResult.Tags.ToList();
        }

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var testResult = await _context.SavedTestResults
            .Include(r => r.Tags)
            .FirstOrDefaultAsync(r => r.Id == id);
        
        if (testResult == null)
        {
            return false;
        }

        _context.SavedTestResults.Remove(testResult);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> BulkDeleteAsync(List<Guid> ids)
    {
        var testResults = await _context.SavedTestResults
            .Where(r => ids.Contains(r.Id))
            .ToListAsync();

        _context.SavedTestResults.RemoveRange(testResults);
        await _context.SaveChangesAsync();
        
        return testResults.Count;
    }

    public async Task<List<string>> GetAllTagsAsync()
    {
        return await _context.SavedTestResultTags
            .Select(t => t.Tag)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }

    public async Task<(int TotalTests, int SuccessfulTests, int FailedTests, double AverageExecutionTime)> GetStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? profileHash = null)
    {
        var query = _context.SavedTestResults.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.ExecutedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.ExecutedAt <= toDate.Value);
        }

        if (!string.IsNullOrEmpty(profileHash))
        {
            query = query.Where(r => r.ProfileHash == profileHash);
        }

        var totalTests = await query.CountAsync();
        var successfulTests = await query.CountAsync(r => r.Success);
        var failedTests = totalTests - successfulTests;
        var averageExecutionTime = totalTests > 0 ? await query.AverageAsync(r => (double)r.Duration) : 0;

        return (totalTests, successfulTests, failedTests, averageExecutionTime);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.SavedTestResults.AnyAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<SavedTestResult>> GetForComparisonAsync(List<Guid> ids)
    {
        return await _context.SavedTestResults
            .Include(r => r.Tags)
            .Include(r => r.CreatedByUser)
            .Where(r => ids.Contains(r.Id))
            .ToListAsync();
    }
} 