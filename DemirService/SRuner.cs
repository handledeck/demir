using DemirService;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using SmartUlcService.NLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using WindowsService1;


namespace SmartUlcService.ScheduleJob
{
  public class UlcScheduleJob
  {
    ISchedulerFactory __schedulerFactory = null;
    IScheduler __scheduler = null;
    public IJobDetail __jDtCron = null;
    public ITrigger __tgrCron = null;
    public string __schedule;
    public bool __first_time_running = false;
    public CancellationToken __stoppingToken;
    public Power __power;
    public UlcScheduleJob(string schedule, CancellationToken stoppingToken)
    {
      this.__schedule = schedule;
      __stoppingToken = stoppingToken;
      __power = new Power(Worker.__configIni);
    }

    public void Stop()
    {
      Worker.__service_run = false;
      //UlcSrvLog.Logger.Info("Service shutdown");
      __scheduler.Shutdown(false);
    }

    public async Task Start()
    {
      LogProvider.IsDisabled = true;
      __schedulerFactory = new StdSchedulerFactory();
      __scheduler = await __schedulerFactory.GetScheduler();
      Dictionary<string, object> dic = new Dictionary<string, object>();
      dic.Add("scheduler", this);
      JobDataMap jobDataMap = new JobDataMap((IDictionary<string, object>)dic);
      IJobDetail jDtIm = JobBuilder.Create<UlcScedilerJob>()
            .WithIdentity("jDtIm")
            .SetJobData(jobDataMap)
            .Build();
      ITrigger tgrIm = TriggerBuilder.Create()
                .WithIdentity("tgrIm")
               .Build();
      await __scheduler.ScheduleJob(jDtIm, tgrIm);


      __jDtCron = JobBuilder.Create<UlcScedilerJob>()
        .SetJobData(jobDataMap)
      .WithIdentity("jDtCron")
      .Build();

      __tgrCron = TriggerBuilder.Create()
          .ForJob(__jDtCron)

         .WithCronSchedule(__schedule)//"* * * * * ?"
        //  .WithIdentity("UlcTrigger")
        //   .WithSimpleSchedule(x => x
        //.WithIntervalInSeconds(10)
        //.RepeatForever())
          //.StartNow()
          .Build();
      Worker.__service_run = true;
      //UlcSrvLog.Logger.Info("Service start");
      await __scheduler.StartDelayed(TimeSpan.FromSeconds(10));
      await __scheduler.Start();
    }

    internal class UlcScedilerJob : IJob
    {


      async Task IJob.Execute(IJobExecutionContext context)
      {
        try
        {
          UlcScheduleJob c = (UlcScheduleJob)context.MergedJobDataMap["scheduler"];
          if (Worker.__intWait > 0)
          {
            UlcSrvLog.Logger.Info("Ожидание окончания опроса");
            return;
          }
          else if (c.__first_time_running)
          {
            UlcSrvLog.Logger.Info("Опрос устройств по расписанию");
          }

          Worker.__cout_request = 0;
          Interlocked.Increment(ref Worker.__intWait);
          UlcSrvLog.Logger.Info("...............Старт расписания опроса...................");
          if (!c.__first_time_running)
          {

            UlcSrvLog.Logger.Info("Первичный опрос устройств");
          }

          Task t = new Task(() => {
            c.__power.RunService(c.__stoppingToken);
            Interlocked.Decrement(ref Worker.__intWait);
          });
          t.Start();
          if (!c.__first_time_running)
          {
            c.__first_time_running = true;
            await context.Scheduler.Clear();
            await context.Scheduler.ScheduleJob(c.__jDtCron, c.__tgrCron);
          }
        }
        catch (Exception q)
        {

          UlcSrvLog.Logger.Info(q, "Ошибка службы");

        }
      }
    }
  }
}


