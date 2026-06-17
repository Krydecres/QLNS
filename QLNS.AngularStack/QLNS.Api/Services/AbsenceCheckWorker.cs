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
        // === HƯỚNG A: "CATCH-UP" KHI KHỞI ĐỘNG LẠI ===
        // Khi API vừa bật lên (sau khi tắt máy hoặc restart server),
        // quét lại 24 giờ qua để bù vắng mặt cho các ca đã kết thúc mà bị bỏ lỡ.
        _logger.LogInformation("AbsenceCheckWorker starting. Running catch-up scan for the last 24 hours...");
        try
        {
            await ScanAndMarkAbsencesAsync(DateTime.Now.AddDays(-1), DateTime.Now, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during catch-up scan on startup.");
        }

        // === VÒNG LẶP CHÍNH: QUÉT MỖI PHÚT ===
        while (!stoppingToken.IsCancellationRequested)
        {
            // Chờ đến đầu phút tiếp theo để quét đúng thời điểm ca kết thúc
            var now = DateTime.Now;
            var nextMinute = now.AddSeconds(60 - now.Second).AddMilliseconds(-now.Millisecond);
            var delay = nextMinute - now;
            if (delay.TotalMilliseconds > 0)
            {
                await Task.Delay(delay, stoppingToken);
            }

            if (stoppingToken.IsCancellationRequested) break;

            var scanTime = DateTime.Now;
            _logger.LogDebug("AbsenceCheckWorker tick at {time}", scanTime);

            try
            {
                // Chỉ quét trong khoảng thời gian của phút vừa trôi qua
                var from = scanTime.AddMinutes(-1);
                await ScanAndMarkAbsencesAsync(from, scanTime, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic absence scan.");
            }
        }
    }

    /// <summary>
    /// Quét tất cả ca làm (EmployeeShift) có EndTime nằm trong khoảng [from, to].
    /// Với mỗi ca đã kết thúc: nếu nhân viên không có check-in → đánh "Vắng mặt".
    /// </summary>
    private async Task ScanAndMarkAbsencesAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Lấy tất cả ngày làm việc cần kiểm tra (có thể trải dài 2 ngày khi catch-up)
        var datesToCheck = new List<DateTime>();
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
        {
            datesToCheck.Add(d);
        }

        foreach (var workDate in datesToCheck)
        {
            // Lấy tất cả EmployeeShift active trong ngày workDate về C# trước
            // (EF Core không thể dịch DateTime.Add(TimeSpan) sang SQL nên phải lọc giờ ở C#)
            var allShiftAssignments = await context.EmployeeShifts
                .Include(es => es.Shift)
                .Include(es => es.Employee)
                .Where(es =>
                    es.IsActive &&
                    es.WorkDate.Date == workDate.Date &&
                    es.Shift != null)
                .ToListAsync(ct);

            // Lọc ở phía C#: chỉ lấy ca có EndTime nằm trong khoảng [from, to]
            var fromTime = from.TimeOfDay;
            var toTime = to.TimeOfDay;

            var endedShiftAssignments = allShiftAssignments
                .Where(es =>
                    es.Shift != null &&
                    es.Shift.EndTime >= fromTime &&
                    es.Shift.EndTime <= toTime)
                .ToList();


            if (!endedShiftAssignments.Any()) continue;

            _logger.LogInformation(
                "Found {count} ended shift assignment(s) on {date} to process.",
                endedShiftAssignments.Count, workDate.ToString("dd/MM/yyyy"));

            foreach (var assignment in endedShiftAssignments)
            {
                if (assignment.Shift == null) continue;

                // Kiểm tra xem nhân viên này đã có Timekeeping cho ca này chưa
                var existing = await context.Timekeepings
                    .FirstOrDefaultAsync(t =>
                        t.EmployeeId == assignment.EmployeeId &&
                        t.ShiftId == assignment.ShiftId &&
                        t.Date.Date == workDate.Date, ct);

                if (existing == null)
                {
                    // Chưa có bản ghi nào → tạo mới với trạng thái "Vắng mặt"
                    context.Timekeepings.Add(new Timekeeping
                    {
                        EmployeeId = assignment.EmployeeId,
                        ShiftId = assignment.ShiftId,
                        Date = workDate.Date,
                        CheckInTime = null,
                        CheckOutTime = null,
                        Status = "Vắng mặt",
                        Note = $"Hệ thống tự động: không check-in ca '{assignment.Shift.Name}' kết thúc lúc {assignment.Shift.EndTime:hh\\:mm}"
                    });

                    _logger.LogInformation(
                        "Marked ABSENT: Employee {employeeId} ({name}), Shift '{shift}', Date {date}.",
                        assignment.EmployeeId,
                        assignment.Employee?.FullName ?? "?",
                        assignment.Shift.Name,
                        workDate.ToString("dd/MM/yyyy"));
                }
                else if (existing.CheckInTime == null)
                {
                    // Đã có bản ghi nhưng không có giờ check-in → cập nhật trạng thái
                    existing.Status = "Vắng mặt";
                    existing.Note = $"Hệ thống tự động: không check-in ca '{assignment.Shift.Name}' kết thúc lúc {assignment.Shift.EndTime:hh\\:mm}";
                    context.Update(existing);

                    _logger.LogInformation(
                        "Updated to ABSENT: Employee {employeeId} ({name}), Shift '{shift}', Date {date}.",
                        assignment.EmployeeId,
                        assignment.Employee?.FullName ?? "?",
                        assignment.Shift.Name,
                        workDate.ToString("dd/MM/yyyy"));
                }
                // Nếu đã có CheckInTime → nhân viên đã check-in, bỏ qua
            }

            await context.SaveChangesAsync(ct);
        }
    }
}
