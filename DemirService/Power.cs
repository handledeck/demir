using DemirService.Db;
using SmartUlcService.ini;
using SmartUlcService.NLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemirService
{
  public class Power
  {
    ConfigIni __configIni;
    public Power(ConfigIni configIni)
    {
      __configIni = configIni;
    }

    public void RunService(CancellationToken stoppingToken) {

      Drivers drivers = new Drivers();
      drivers.RequestPower(this.__configIni, "80106864");
    }
  }
}
