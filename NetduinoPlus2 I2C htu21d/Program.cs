using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;



namespace NetduinoPlus2_I2C_htu21d
{
    public class Program
    {
        public static void Main()
        {
            //
            //conection:
            //3.3 => 3.3
            //GND => GND
            //SDA => SD
            //SCL => SC
            //No pull-up resistor required if you buy model from sparkfun (https://www.sparkfun.com/products/12064)

            //v0.1
            //Ok: Humidity, temperature
            //ToDo: check_crc, setResolution

            Htu21d sensor = new Htu21d();
            while (true)
            {
                Debug.Print("Humidity    " + sensor.readHumidity().ToString("n2") + "%");
                Debug.Print("Temperature " + sensor.readTemperature().ToString("n2") + "C");
                Thread.Sleep(500);
            }

        }

    }
}
