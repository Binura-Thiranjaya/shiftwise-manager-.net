using TandTFuel.Api.Models;

namespace TandTFuel.Api.Services.Payroll;

public interface IPayrollService
{
    Task<Payslip> GeneratePayslipAsync(Guid employeeId, DateTime periodStart, DateTime periodEnd);
}