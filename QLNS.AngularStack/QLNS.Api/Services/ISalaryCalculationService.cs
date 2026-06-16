using QLNS.FullNet.Data.Entities;

namespace QLNS.Api.Services;

public interface ISalaryCalculationService
{
    Task<Salary?> CalculateAsync(int employeeId, int month, int year);
    Task<List<Salary>> CalculateForAllAsync(int month, int year);
}