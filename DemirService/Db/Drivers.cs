using ConsoleApp26;
using InterUlc.Drivers;
using SmartUlcService.ini;
using SmartUlcService.NLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DemirService.Db
{
  public class Drivers
  {
    public static TcpClient GetConnection(string host, int port)
    {
      TcpClient client;
      try
      {
        client = new TcpClient();
        IAsyncResult result = client.BeginConnect(host, port, (i) =>
        {
          if (client.Client != null)
          {
            if (!client.Connected)
            {
              client = null;
            }
          }
        }, null);
        bool state = result.AsyncWaitHandle.WaitOne(5000);
        if (!state)
          return null;
        else return client;
      }
      catch (Exception)
      {
        return null;
      }
    }

    public void RequestPower(ConfigIni configIni,string num_conter)
    {
      db d = new db("Server=localhost;Port=5432;Userid=postgres;Password=root;Timeout=15;Database=demir");
      TcpClient tcpClient = null;
      try
      {
        tcpClient = Drivers.GetConnection(configIni.IpDemir, 10250);
        if (tcpClient == null)
          throw new Exception();
        float fl;
        bool result = EnMera318BY.GetValue(EnMera318Fun.PowerWing, num_conter, tcpClient, 10000, out fl);
        if (!result)
        {
          throw new Exception();
        }
        UlcSrvLog.Logger.Info(DateTime.Now.ToString("dd.MM.yy HH:mm:ss") + " data:" + fl.ToString());

        d.WriteData(fl);
       
      }
      catch(Exception exp)
      {
        UlcSrvLog.Logger.Info("Ошибка чтения из счетчика");
      }
     
    }
  }
}
