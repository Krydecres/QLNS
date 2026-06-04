
namespace QLNS.FullNet.Services
{
    public class SalaryCalculationService
    {
        public decimal CalculateTotal(decimal basicSalary, double coefficient, decimal allowance)
        {
            return (decimal)coefficient * basicSalary + allowance;
        }

        public void UpdateSalary(Salary salary)
        {
            salary.TotalSalary = CalculateTotal(
                salary.BasicSalary,
                salary.Coefficient,
                salary.Allowance
            );
        }
    }
}