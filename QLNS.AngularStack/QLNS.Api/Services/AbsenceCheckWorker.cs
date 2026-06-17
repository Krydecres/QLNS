using QLNS.Api.Data;
using QLNS.FullNet.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace QLNS.Api.Services;

public class AbsenceCheckWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AbsenceCheckWorker> _logger;

    public AbsenceCheckWorker(IServiceProvider serviceProvider, ILogger<AbsenceCheckWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;

            // Check if it's 17:00 (we allow 1 minute window)
            if (now.Hour == 17 && now.Minute == 0)
            {
                _logger.LogInformation("AbsenceCheckWorker running at {time}", DateTimeOffset.Now);
                try
                {
                    await CheckAbsencesAsync(now.Date);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing AbsenceCheckWorker.");
                }

                // Wait 60 seconds to avoid running again within the same minute
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
            else
            {
                // Wait for the next minute
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task CheckAbsencesAsync(DateTime targetDate)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var employees = await context.Employees.ToListAsync();

        foreach (var employee in employees)
        {
            // Kiểm tra xem nhân viên này có ca làm việc nào trong ngày hôm nay không
            var hasShiftToday = await context.EmployeeShifts
                .Include(es => es.Shift)
                .AnyAsync(es => es.EmployeeId == employee.Id && es.WorkDate.Date == targetDate);

            // Kiểm tra xem đã có timekeeping chưa
            var timekeeping = await context.Timekeepings
                .FirstOrDefaultAsync(t => t.EmployeeId == employee.Id && t.Date.Date == targetDate);

            if (timekeeping == null)
            {
                string status = hasShiftToday ? "Vắng mặt" : "Chưa chấm công";
                
                var newRecord = new Timekeeping
                {
                    EmployeeId = employee.Id,
                    Date = targetDate,
                    CheckInTime = null,
                    CheckOutTime = null,
                    Status = status,
                    Note = "Hệ thống tự động cập nhật lúc 17h"
                };
                context.Timekeepings.Add(newRecord);
            }
            else if (timekeeping.CheckInTime == null)
            {
                // Có record nhưng ko có giờ check in -> Cập nhật trạng thái
                timekeeping.Status = hasShiftToday ? "Vắng mặt" : "Chưa chấm công";
                timekeeping.Note = "Hệ thống tự động cập nhật lúc 17h (Không có CheckIn)";
                context.Update(timekeeping);
            }
        }

        await context.SaveChangesAsync();
    }
}
