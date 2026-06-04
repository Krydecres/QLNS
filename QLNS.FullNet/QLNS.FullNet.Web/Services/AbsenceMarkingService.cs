using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data;
using QLNS.FullNet.Data.Entities;

namespace QLNS.FullNet.Web.Services
{
    /// <summary>
    /// Background service tự động chấm vắng mặt mỗi ngày lúc 17:00.
    /// Với mỗi nhân viên chưa có bản ghi check-in hôm nay, sẽ tạo bản ghi "Vắng mặt".
    /// </summary>
    public class AbsenceMarkingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AbsenceMarkingService> _logger;

        // Giờ kích hoạt đánh dấu vắng mặt tự động (17:00)
        private static readonly TimeSpan TriggerTime = new TimeSpan(17, 0, 0);

        public AbsenceMarkingService(IServiceProvider serviceProvider, ILogger<AbsenceMarkingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AbsenceMarkingService đã khởi động.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var todayTrigger = now.Date + TriggerTime;

                // Tính thời gian chờ đến lần kích hoạt tiếp theo
                TimeSpan delay;
                if (now < todayTrigger)
                {
                    // Hôm nay chưa đến 17:00 → chờ đến 17:00 hôm nay
                    delay = todayTrigger - now;
                }
                else
                {
                    // Đã qua 17:00 → chờ đến 17:00 ngày mai
                    delay = todayTrigger.AddDays(1) - now;
                }

                _logger.LogInformation(
                    "AbsenceMarkingService: Lần chấm vắng mặt tiếp theo lúc {NextRun:dd/MM/yyyy HH:mm}.",
                    now + delay);

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await MarkAbsentEmployeesAsync(DateTime.Today);
                }
            }
        }

        /// <summary>
        /// Tạo bản ghi "Vắng mặt" cho tất cả nhân viên chưa có bản ghi chấm công trong ngày <paramref name="date"/>.
        /// </summary>
        public async Task<int> MarkAbsentEmployeesAsync(DateTime date)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var allEmployees = await context.Employees.AsNoTracking().ToListAsync();

                var checkedInIds = await context.Timekeepings
                    .Where(t => t.Date.Date == date.Date)
                    .Select(t => t.EmployeeId)
                    .ToListAsync();

                var absentEmployees = allEmployees
                    .Where(e => !checkedInIds.Contains(e.Id))
                    .ToList();

                if (!absentEmployees.Any())
                {
                    _logger.LogInformation(
                        "AbsenceMarkingService: Không có nhân viên vắng mặt ngày {Date:dd/MM/yyyy}.",
                        date);
                    return 0;
                }

                var absentRecords = absentEmployees.Select(e => new Timekeeping
                {
                    EmployeeId = e.Id,
                    Date = date,
                    CheckInTime = null,
                    CheckOutTime = null,
                    Status = "Vắng mặt",
                    Note = "Tự động chấm vắng mặt lúc 17:00"
                }).ToList();

                context.Timekeepings.AddRange(absentRecords);
                await context.SaveChangesAsync();

                _logger.LogInformation(
                    "AbsenceMarkingService: Đã chấm vắng mặt cho {Count} nhân viên ngày {Date:dd/MM/yyyy}.",
                    absentRecords.Count, date);

                return absentRecords.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AbsenceMarkingService: Lỗi khi chấm vắng mặt ngày {Date:dd/MM/yyyy}.", date);
                return 0;
            }
        }
    }
}
