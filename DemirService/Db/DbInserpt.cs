using DemirService.Db;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp26
{
  public class db
  {
    string __connection;
    public db(string connection)
    {
      this.__connection = connection;
    }

    public void WriteData(float val)
    {
      try
      {
        var dbFactory = new ServiceStack.OrmLite.OrmLiteConnectionFactory(
      __connection, PostgreSqlDialect.Provider);
        using (var db = dbFactory.Open())
        {
          dimir dbValue = new dimir()
          {
            datetime = DateTime.Now,
            value = val
          };
          db.Insert<dimir>(dbValue);
        }
      }
      catch (Exception exp)
      {

        int x = 0;
      }
    }
  }
}
