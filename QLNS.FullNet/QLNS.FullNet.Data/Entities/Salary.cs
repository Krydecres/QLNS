namespace QLNS.FullNet.Data.Entities
{
    public class Salary
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public int Month { get; set; }
        public int Year { get; set; }

        public decimal BaseSalary { get; set; }
        public decimal Allowance { get; set; }
        public decimal Deduction { get; set; }
        public decimal TotalSalary { get; set; }
    }
}