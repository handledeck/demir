using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace InterUlc.Drivers
{

  public enum EnMera318Fun
  {
    EnergyStartDay,
    EnergyStartMonth,
    PowerWing
  }
  public class EnMera318BY
  {
    static readonly UInt16 NCP_CRC_POLYNOM = 0x8005;//для CRC
    static readonly byte[] funcEnergyStartDay = { 0x0B, 0x00, 0x01, 0x02, 0x00, 0x01, 0x01 };// энергия на начало суток
    static readonly byte[] funcEnergyStartMonth = { 0x0B, 0x00, 0x0D, 0x02, 0x00, 0x01, 0x01 }; // энергия на начало месяца
    static readonly byte[] funcPowerNow = { 0x0A, 0x00, 0x0D, 0x1C }; //текущая мгновенная мощность по трем фазам

    static uint? GetUintFactory(string serial_number)
    {
      uint addr;
      uint? result = null;

      if (uint.TryParse(serial_number, out addr))
        result = addr;
      return result;
    }
    public static MeterAllValues GetSumAllValue(string meter_factory, TcpClient client)
    {
      Exception exc = null;
      MeterAllValues meterAllValues = new MeterAllValues();
      float day = 0;
      float month = 0;
      if (GetValue(EnMera318Fun.EnergyStartDay, meter_factory, client, 10000, out day))
        meterAllValues.EnergySumDay = day;
      else
        return null;
      if (GetValue(EnMera318Fun.EnergyStartMonth, meter_factory, client, 10000, out month))
        meterAllValues.EnergySumMonth = month;
      else
        return null;
      return meterAllValues;
    }


    public static bool GetValue(EnMera318Fun enMera318Fun, string factory_number, TcpClient tcpClient, int readTimeout, out float value)
    {
      bool result = false;
      value = 0;
      try
      {
        uint? serial_number = GetUintFactory(factory_number);
        if (!serial_number.HasValue)
          return false;
        byte[] reqPack = null;
        byte[] response = new byte[256];
        switch (enMera318Fun)
        {
          case EnMera318Fun.EnergyStartDay:
            reqPack = PreparePack(funcEnergyStartDay, serial_number.Value);
            break;
          case EnMera318Fun.EnergyStartMonth:
            reqPack = PreparePack(funcEnergyStartMonth, serial_number.Value);
            break;
          case EnMera318Fun.PowerWing:
            reqPack = PreparePack(funcPowerNow, serial_number.Value);
            break;
          default:
            break;
        }
        NetworkStream networkStream = tcpClient.GetStream();
        networkStream.ReadTimeout = readTimeout;
        networkStream.Write(reqPack, 0, reqPack.Length);
        int len = networkStream.Read(response, 0, response.Length);
        byte[] data = PackCheck(response, len);
        if (data[0] == 0x0B) //multiVal
        {
          float[] res = null;
          data = data.Skip<byte>(2).ToArray<byte>();
          switch (data[0])
          {
            case 0x01: //energy start day
              data = data.Skip<byte>(1).ToArray<byte>();
              res = GetMultiVal(data, 1);
              break;
            case 0x02: //energy start month
              data = data.Skip<byte>(1).ToArray<byte>();
              res = GetMultiVal(data, 1);
              break;
          }
          if (res == null)
          {
            value = 0;
            result = false;
          }
          else
          {
            value = res[0];
            result = true;
          }
        }
        else if (data[0] == 0x0A)
        {
          data = data.Skip<byte>(2).ToArray<byte>();

          float[] res = new float[3];
          switch (data[0])
          {
            case 0x0D: //Мгновенная мощность по трем фазам
              data = data.Skip<byte>(1).ToArray<byte>();
              ushort index = 0;

              res[0] = getValue(data, ref index);
              res[1] = getValue(data, ref index);
              res[2] = getValue(data, ref index);
              value = res[0] + res[1] + res[2];
              result = true;
              //Console.WriteLine(string.Format("A:{0} B:{1} C:{2}",res[0], res[1] , res[2]));
              //Console.WriteLine($"sum = {res[0] + res[1] + res[2]}");
              break;
          }
        }
        return result;
      }
      catch (Exception exp)
      {
        return false;
      }
    }


    //todo отправка запроса 
    //todo приемка ответа
    //todo парсинг ответа
    static byte[] PreparePack(byte[] func, UInt32 MeterNum)
    {
      byte[] num = BitConverter.GetBytes(MeterNum);
      byte[] body = new byte[num.Length + func.Length + 5];

      int i = 0;
      body[i++] = 0x06;
      Array.Copy(num, 0, body, i, num.Length);
      i += num.Length;
      body[i++] = 0x00;
      body[i++] = 0x06;
      Array.Copy(func, 0, body, i, func.Length);
      i += func.Length;
      UInt16 crc = CalcCRC(body, (ushort)i);
      body[i++] = (byte)((crc >> 8) & 0xff);
      body[i++] = (byte)(crc & 0xff);
      body = SetEscape(body);
      byte[] res = new byte[body.Length + 2];
      res[0] = 0xC0;
      Array.Copy(body, 0, res, 1, body.Length);
      res[res.Length - 1] = 0xC0;
      return res;
    }

    //Парсинг пакета
    // 1 обрезать обрамление 0xC0
    // 2 Посчитать CRC
    // 3 Парсинг типа данных
    // 3 выцепить данные из пакета в сыром виде
    // 4 преобразование в значение Float
    static byte[] PackCheck(byte[] data, int length)
    {
      byte[] res = new byte[length - 2];
      Array.Copy(data, 1, res, 0, length - 2);
      int Len = res.Length;
      res = DeleteEscapeCharacters(res, ref Len);
      UInt16 crc = CalcCRC(res, (ushort)(res.Length - 2));
      UInt16 crc_req = (UInt16)((ushort)res[res.Length - 1] | (res[res.Length - 2] << 8));
      if (crc != crc_req)
      {
        return null;
      }
      Array.Resize<byte>(ref res, res.Length - 2);
      res = res.Skip<byte>(7).ToArray<byte>();
      return res; // массив 
    }

    static float[] GetMultiVal(byte[] data, int count)
    {
      float[] res = new float[count];
      UInt16 index = 0;
      for (int i = 0; i < count; ++i)
      {
        UInt32 val = getValue(data, ref index);
        byte st = 0;
        res[i] = GetMultValueStatus(val, out st);
      }
      return res;
    }

    static byte[] SetEscape(byte[] data)
    {
      byte[] res = new byte[data.Length * 2];
      int i;
      int j = 0;

      for (i = 0; i < data.Length; ++i)
      {
        if (data[i] == 0xC0)
        {
          res[j++] = 0xDB;
          res[j++] = 0xDC;
        }
        else if (data[i] == 0xDB)
        {
          res[j++] = 0xDB;
          res[j++] = 0xDD;
        }
        else
          res[j++] = data[i];
      }
      Array.Resize<byte>(ref res, j);
      return res;
    }

    static UInt16 CalcCRC(byte[] buff, UInt16 size)
    {

      byte i;
      UInt16 j = 0;
      UInt16 crc = 0;
      while (size-- > 0)
      {
        crc ^= (UInt16)(((UInt16)buff[j++]) << 8);
        for (i = 0; i < 8; i++)
        {
          crc = ((crc & 0x8000) != 0) ? (UInt16)((crc << 1) ^ NCP_CRC_POLYNOM) : (UInt16)(crc << 1);
        }
      }

      return crc;
    }

    static UInt32 getValue(byte[] buf, ref UInt16 StartIndex)
    {
      int i = 0;
      UInt32 val = 0;

      do
      {
        val |= (UInt32)(((buf[StartIndex] & 0x7f)) << (7 * i));
        ++i;
      } while ((buf[StartIndex++] & 0x80) != 0);

      return val;
    }

    static float GetMultValueStatus(UInt32 val, out byte status)
    {
      float res = 0.0f;

      status = (byte)(val & 0x07);
      res = (val >> 3) / 10000.0f;
      return res;
    }

    static byte[] DeleteEscapeCharacters(byte[] data, ref int length)
    {
      int len = data.Length;
      byte[] res = new byte[len];

      int i = 0;
      int j = 0;
      while (i < data.Length)
      {
        if (data[i] == 0xDB)
        {
          res[j] = (byte)((data[i + 1] == 0xDC) ? 0xc0 : 0xDB);
          len--;
          j++;
          i += 2;
        }
        else
        {
          res[j] = data[i];
          i++;
          j++;
        }
      }
      Array.Resize<byte>(ref res, len);
      length = len;
      return res;
    }
  }
}
