using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;

namespace QLNS.FullNet.Web.Services
{
    public class SalaryCalculationService : ISalaryCalculationService
    {
        private readonly AppDbContext _context;

        public SalaryCalculationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Salary?> CalculateAsync(Employee employee, int month, int year)
        {
            int standardWorkingDays = 22;

            var timekeepings = await _context.Timekeepings
                .Where(t => t.EmployeeId == employee.Id && t.Date.Month == month && t.Date.Year == year)
                .ToListAsync();

            int actualWorkingDays = timekeepings.Count(t => t.CheckInTime != null);

            decimal baseSalary = employee.BaseSalary;
            decimal allowance = employee.Allowance;
            
            decimal totalSalary = (baseSalary / standardWorkingDays) * actualWorkingDays + allowance;

            if (totalSalary < 0) totalSalary = 0;

            return new Salary
            {
                EmployeeId = employee.Id,
                Month = month,
                Year = year,
                BaseSalary = baseSalary,
                Allowance = allowance,
                Deduction = 0,
                TotalSalary = totalSalary
            };
        }
    }
}