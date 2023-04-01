using SmartUlcService.ini;
using SmartUlcService.NLogs;
using SmartUlcService.ScheduleJob;

namespace DemirService
{
  public class Worker : BackgroundService
  {
    private readonly ILogger<Worker> _logger;
    public static ConfigIni __configIni;
    public static int __intWait = 0;
    public static int __cout_request = 0;
    public static int __cout_ulc_request = 0;
    public static int __cout_ulc_meters = 0;
    public static int __cout_ulc_rs485 = 0;
    public static bool __service_run = false;
    UlcScheduleJob __ulcScheduleJob;
    public Worker(ILogger<Worker> logger)
    {
      _logger = logger;
      __configIni = new ConfigIni();
      UlcSrvLog.InitUlcSrvLog();
      UlcSrvLog.Logger.Info("Инициализация службы");
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
      _logger.LogInformation("Stopping Windows Service...");
      return base.StopAsync(cancellationToken);
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

      UlcSrvLog.Logger.Info("Старт службы");
      __ulcScheduleJob = new UlcScheduleJob(Worker.__configIni.Scheduler, stoppingToken);
      await __ulcScheduleJob.Start();
    }
  }
}