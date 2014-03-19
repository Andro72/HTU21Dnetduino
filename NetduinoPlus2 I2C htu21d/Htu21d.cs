using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;

namespace NetduinoPlus2_I2C_htu21d
{

    class Htu21d
    {
        const Byte HTDU21D_ADDRESS = 0x40;  //Unshifted 7-bit I2C address for the sensor

        private const Byte TRIGGER_TEMP_MEASURE_HOLD = 0xE3;
        private const Byte TRIGGER_HUMD_MEASURE_HOLD = 0xE5;
        private Byte[] TRIGGER_TEMP_MEASURE_NOHOLD = {0xF3};
        private Byte[] TRIGGER_HUMD_MEASURE_NOHOLD = {0xF5};
        private const Byte WRITE_USER_REG = 0xE6;
        private const Byte READ_USER_REG = 0xE7;
        private const Byte SOFT_RESET = 0x0F;

        private const int DefaultClockRate = 400;
        private const int TransactionTimeout = 1000;

        private const Int32 SHIFTED_DIVISOR = 0x988000;

        static I2CDevice i2cDevice = null;
        

        //Public Functions
        public Htu21d()
        {
            i2cDevice = new I2CDevice(new I2CDevice.Configuration(HTDU21D_ADDRESS, DefaultClockRate));

           //Thread.Sleep(100);
        }
        
        private void write(byte[] writeBuffer)
        {
            I2CDevice.I2CTransaction[] xActions = new I2CDevice.I2CTransaction[1];
            xActions[0] = I2CDevice.CreateWriteTransaction(writeBuffer);


            int written = i2cDevice.Execute(xActions, TransactionTimeout);
            if (written == 0)
            {
                Debug.Print("Failed to perform I2C transaction. Check version of Netduino firmaware. And cable connection.");
            }
            else
            {
                Debug.Print("Send ok ");
            }
        }
        private void read(byte[] readBuffer)
        {
            // create a read transaction
            I2CDevice.I2CTransaction[] readTransaction = new I2CDevice.I2CTransaction[]
            {
                I2CDevice.CreateReadTransaction(readBuffer)
            };

            // read data from the device
            int read = i2cDevice.Execute(readTransaction, TransactionTimeout);

            // make sure the data was read
            if (read != readBuffer.Length)
            {
                throw new Exception("Could not read from device.");
            }
            else
            {
                Debug.Print("Read ok ");
            }
        }


        public float readHumidity()
        {
            write(TRIGGER_HUMD_MEASURE_NOHOLD);

            //Request a humidity reading
            //Wire.beginTransmission(HTDU21D_ADDRESS);
            //Wire.write(TRIGGER_HUMD_MEASURE_NOHOLD); //Measure humidity with no bus holding
            //Wire.endTransmission();

	        //Hang out while measurement is taken. 50mS max, page 4 of datasheet.
            //delay(55);
            Thread.Sleep(100);

	        //Comes back in three bytes, data(MSB) / data(LSB) / Checksum
            //Wire.requestFrom(HTDU21D_ADDRESS, 3);
            byte[] readHum = new byte[3];
            read(readHum);

            byte msb = readHum[0];
            byte lsb = readHum[1];
            byte checksum = readHum[2];

            ////Wait for data to become available
            //int counter = 0;
            //while(Wire.available() < 3)
            //{
            //    counter++;
            //    delay(1);
            //    if(counter > 100) return 998; //Error out
            //}

            //byte msb, lsb, checksum;
            //msb = Wire.read();
            //lsb = Wire.read();
            //checksum = Wire.read();

	        /* //Used for testing
	        byte msb, lsb, checksum;
	        msb = 0x4E;
	        lsb = 0x85;
	        checksum = 0x6B;*/
	
	        uint rawHumidity = ((uint) msb << 8) | (uint) lsb;

            //if (check_crc(rawHumidity, checksum) != 0) return (999); //Error out

	        //sensorStatus = rawHumidity & 0x0003; //Grab only the right two bits
	        rawHumidity &= 0xFFFC; //Zero out the status bits but keep them in place
	
	        //Given the raw humidity data, calculate the actual relative humidity
	        float tempRH = rawHumidity / (float)65536; //2^16 = 65536
	        float rh = -6 + (125 * tempRH); //From page 14
	
	        return(rh);

        }


        public float readTemperature()
        {
            write(TRIGGER_TEMP_MEASURE_NOHOLD);
            Thread.Sleep(55);
            byte[] readTemp = new byte[3];
            read(readTemp);

            byte msb = readTemp[0];
            byte lsb = readTemp[1];
            byte checksum = readTemp[2];


	        /* //Used for testing
	        byte msb, lsb, checksum;
	        msb = 0x68;
	        lsb = 0x3A;
	        checksum = 0x7C; */

	        uint rawTemperature = ((uint) msb << 8) | (uint) lsb;

	        //if(check_crc(rawTemperature, checksum) != 0) return(999); //Error out

	        //sensorStatus = rawTemperature & 0x0003; //Grab only the right two bits
	        rawTemperature &= 0xFFFC; //Zero out the status bits but keep them in place

	        //Given the raw temperature data, calculate the actual temperature
	        float tempTemperature = rawTemperature / (float)65536; //2^16 = 65536
	        float realTemperature =(float)( -46.85 + (175.72 * tempTemperature)); //From page 14

	        return(realTemperature);  

        }



        //Set sensor resolution
        /*******************************************************************************************/
        //Sets the sensor resolution to one of four levels
        //Page 12:
        // 0/0 = 12bit RH, 14bit Temp
        // 0/1 = 8bit RH, 12bit Temp
        // 1/0 = 10bit RH, 13bit Temp
        // 1/1 = 11bit RH, 11bit Temp
        //Power on default is 0/0

        void setResolution(byte resolution)
        {
          byte userRegister = read_user_register(); //Go get the current register state
          //userRegister &= 0b01111110; //Turn off the resolution bits
          //resolution &= 0b10000001; //Turn off all other bits but resolution bits
          //userRegister |= resolution; //Mask in the requested resolution bits
          userRegister &= 0x73; //Turn off the resolution bits
          resolution &= 0x81; //Turn off all other bits but resolution bits
          userRegister |= resolution; //Mask in the requested resolution bits
  
          //Request a write to user register
          //Wire.beginTransmission(HTDU21D_ADDRESS);
          write(new byte[]{WRITE_USER_REG}); //Write to the user register
          write(new byte[]{userRegister}); //Write the new resolution bits
          //Wire.endTransmission();
        }

        //Read the user register
        byte read_user_register()
        {
            byte userRegister;

            //Request the user register
            write(new byte[]{READ_USER_REG}); //Read the user register
            

            //Read result
            byte[] readData = new byte[1];
            read(readData);
            userRegister = readData[0];

            return (userRegister);
        }

        //Give this function the 2 byte message (measurement) and the check_value byte from the HTU21D
        //If it returns 0, then the transmission was good
        //If it returns something other than 0, then the communication was corrupted
        //From: http://www.nongnu.org/avr-libc/user-manual/group__util__crc.html
        //POLYNOMIAL = 0x0131 = x^8 + x^5 + x^4 + 1 : http://en.wikipedia.org/wiki/Computation_of_cyclic_redundancy_checks
        //This is the 0x0131 polynomial shifted to farthest left of three bytes

        byte check_crc(uint message_from_sensor, uint check_value_from_sensor)
        {
            //Test cases from datasheet:
            //message = 0xDC, checkvalue is 0x79
            //message = 0x683A, checkvalue is 0x7C
            //message = 0x4E85, checkvalue is 0x6B

            UInt32 remainder = (UInt32)message_from_sensor << 8; //Pad with 8 bits because we have to add in the check value
            remainder |= check_value_from_sensor; //Add on the check value

            UInt32 divsor = (UInt32)SHIFTED_DIVISOR;

            for (int i = 0; i < 16; i++) //Operate on only 16 positions of max 24. The remaining 8 are our remainder and should be zero when we're done.
            {
                //Serial.print("remainder: ");
                //Serial.println(remainder, BIN);
                //Serial.print("divsor:    ");
                //Serial.println(divsor, BIN);
                //Serial.println();

                if ((remainder & (uint)1 << (23 - i))==1) //Check if there is a one in the left position
                    remainder ^= divsor;

                divsor >>= 1; //Rotate the divsor max 16 times so that we have 8 bits left of a remainder
            }

            return (byte)remainder;
        }



    }
}
