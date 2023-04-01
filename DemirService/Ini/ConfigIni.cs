using IniParser.Parser;
using SmartUlcService.NLogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SmartUlcService.ini
{
  public class ConfigIni
  {
    const string __ini_file = "UlcSrvSettings.ini";
    string __ini_path_file = string.Empty;

    public string IpDb { get; set; }
    public int Port { get; set; }
    public string UserDb { get; set; }
    public string UserPwdDb { get; set; }
    public string Scheduler { get; set; }
    public string IpDemir { get; set; }

    public static string AssemblyDirectory
    {
      get
      {
        string codeBase = Assembly.GetExecutingAssembly().CodeBase;
        UriBuilder uri = new UriBuilder(codeBase);
        string path = Uri.UnescapeDataString(uri.Path);
        return Path.GetDirectoryName(path);
      }
    }

   

    public ConfigIni()
    {
      this.__ini_path_file = AssemblyDirectory + "\\" + __ini_file;
      
      ReadIniFile();
    }

    void ReadIniFile()
    {
      try
      {
        if (!File.Exists(this.__ini_path_file))
        {
          UlcSrvLog.Logger.Error(string.Format("Не найден файл настройки {0}. Создаю новый по умолчанию.", this.__ini_path_file));
          WriteIniSrv(this.__ini_path_file);
        }
        else
        {
          StreamReader s = new StreamReader(this.__ini_path_file, false);
          IniDataParser p = new IniDataParser();
          IniParser.FileIniDataParser fileIniDataParser = new IniParser.FileIniDataParser();
          var iData = fileIniDataParser.ReadData(s);
          this.IpDb = iData["DB"].GetKeyData("ip").Value;
          this.Port = int.Parse(iData["DB"].GetKeyData("port").Value);
          this.UserDb = iData["DBUser"].GetKeyData("user").Value;
          this.UserPwdDb = iData["DBUser"].GetKeyData("password").Value;
          this.UserPwdDb = iData["DBUser"].GetKeyData("password").Value;
          this.Scheduler = iData["Schedule"].GetKeyData("schedule").Value;
          this.IpDemir= iData["Demir"].GetKeyData("ip_demir").Value;
          s.Close();
        }
      }
      catch (Exception exc)
      {
        UlcSrvLog.Logger.Error(exc.Message);
      }
    }

    void WriteIniSrv(string pathFPath)
    {
      StreamWriter s = new StreamWriter(pathFPath, false);
      IniParser.Model.SectionData db = new IniParser.Model.SectionData("DB");
      IniParser.Model.IniData iniDb = new IniParser.Model.IniData();
      db.Comments.Add("Section Ip address and port  for connection DB");
      db.Keys.AddKey("ip", "127.0.0.1");
      db.Keys.AddKey("port", "5432");
      iniDb.Sections.Add(db);
      IniParser.Model.SectionData dbUser = new IniParser.Model.SectionData("DBUser");
      IniParser.Model.IniData iniUser = new IniParser.Model.IniData();
      dbUser.Comments.Add("Section for user and password for connection DB");
      dbUser.Keys.AddKey("user", "postgres");
      dbUser.Keys.AddKey("password", "root");
      iniDb.Sections.Add(dbUser);
      IniParser.Model.SectionData schedule = new IniParser.Model.SectionData("Schedule");
      schedule.Comments.Add("Section for schedule");
      schedule.Keys.AddKey("schedule", "0 0/5 * * * ?");
      iniDb.Sections.Add(schedule);
      IniParser.Model.SectionData ip_demir = new IniParser.Model.SectionData("Demir");
      ip_demir.Comments.Add("Section for demmir");
      ip_demir.Keys.AddKey("ip_demir", "127.0.0.1");
      iniDb.Sections.Add(ip_demir);

      //string conn = "Server=localhost;Port=5432;Userid=postgres;Password=root;Timeout=15;Database=demir";


      IniParser.FileIniDataParser fileIniDataParser = new IniParser.FileIniDataParser();
      fileIniDataParser.WriteData(s, iniDb);
      s.Flush();
      s.Close();
    }


  }
}
