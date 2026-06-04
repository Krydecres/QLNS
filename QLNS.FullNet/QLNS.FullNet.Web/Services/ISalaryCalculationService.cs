using QLNS.FullNet.Data.Entities;

namespace QLNS.FullNet.Web.Services
{
    public interface ISalaryCalculationService
    {
        Task<Salary?> CalculateAsync(Employee employee, int month, int year);
    }
}
