// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Iot.Device.Bno055;
using System;
using Iot.Device.Ft4222;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Spi;
using System.Threading;
using Iot.Device.Mfrc522;
using Iot.Device.Card.Mifare;
using System.Buffers.Binary;

namespace Ft4222.sample
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello I2C, SPI and GPIO FTFI! FT4222");
            //Console.WriteLine("Select the test you want to run:");
            //Console.WriteLine(" 1 Run I2C tests with a BNO055");
            //Console.WriteLine(" 2 Run SPI tests with a simple HC595 with led blinking on all ports");
            //Console.WriteLine(" 3 Run GPIO tests with a simple led blinking on GPIO2 port and reading the port");
            //Console.WriteLine(" 4 Run callback test event on GPIO2 on Failing and Rising");
            //var key = Console.ReadKey();
            //Console.WriteLine();

            //var devices = FtCommon.GetDevices();
            //Console.WriteLine($"{devices.Count} FT4222 elements found");
            //foreach (var device in devices)
            //{
            //    Console.WriteLine($"Description: {device.Description}");
            //    Console.WriteLine($"Flags: {device.Flags}");
            //    Console.WriteLine($"Id: {device.Id}");
            //    Console.WriteLine($"Location Id: {device.LocId}");
            //    Console.WriteLine($"Serial Number: {device.SerialNumber}");
            //    Console.WriteLine($"Device type: {device.Type}");
            //}

            //var (chip, dll) = FtCommon.GetVersions();
            //Console.WriteLine($"Chip version: {chip}");
            //Console.WriteLine($"Dll version: {dll}");

            //if (key.KeyChar == '1')
            //    TestI2c();

            //if (key.KeyChar == '2')
            //    TestSpi();

            //if (key.KeyChar == '3')
            //    TestGpio();

            //if (key.KeyChar == '4')
            //    TestEvents();

            TestMC522();
        }

        private static void TestMC522()
        {
            var ftSpi = new Ft4222Spi(new SpiConnectionSettings(0, 1) { ClockFrequency = 1_000_000, Mode = SpiMode.Mode0 });
            Mfrc522Controller mfrc522 = new Mfrc522Controller(ftSpi);
            Console.WriteLine($"Version: {mfrc522.Version}");
            byte[] targets = null;
            while (targets == null)
            {
                targets = mfrc522.ListPassiveTarget();                    
                Thread.Sleep(100);
            }
            Console.WriteLine($"{BitConverter.ToString(targets)}");

            MifareCard mifareCard = new MifareCard(mfrc522, 1) { BlockNumber = 0, Command = MifareCardCommand.AuthenticationA };
            mifareCard.SetCapacity((BinaryPrimitives.ReadUInt16BigEndian(targets.AsSpan().Slice(0,2))), targets[2]);
            mifareCard.SerialNumber =targets.AsSpan().Slice(3).ToArray();
            //mifareCard.KeyA = new byte[6] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            mifareCard.KeyA = new byte[6] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };//new byte[6] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            mifareCard.KeyB = new byte[6] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };//new byte[6] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            for (byte block = 0; block < 64; block++)
            {
                mifareCard.BlockNumber = block;
                mifareCard.Command = MifareCardCommand.AuthenticationA;
                var ret = mifareCard.RunMifiCardCommand();
                if (ret < 0)
                {
                    // Try another one
                    mifareCard.Command = MifareCardCommand.AuthenticationB;
                    ret = mifareCard.RunMifiCardCommand();
                }
                //var ret = pn532.InDataExchange(mifareCard.Tg, new byte[] { 0x60, block, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x66, 0x8D, 0x7E, 0x20 }, Span<byte>.Empty);
                if (ret >= 0)
                {
                    mifareCard.BlockNumber = block;
                    mifareCard.Command = MifareCardCommand.Read16Bytes;
                    ret = mifareCard.RunMifiCardCommand();
                    if (ret >= 0)
                        Console.WriteLine($"Bloc: {block}, Data: {BitConverter.ToString(mifareCard.Data)}");
                    else
                    {
                        Console.WriteLine($"Error reading bloc: {block}");
                    }
                    if (block % 4 == 3)
                    {
                        // Check what are the permissions
                        for (byte j = 3; j > 0; j--)
                        {
                            var access = mifareCard.BlockAccess((byte)(block - j), mifareCard.Data);
                            Console.WriteLine($"Bloc: {block - j}, Access: {access}");
                        }
                        var sector = mifareCard.SectorTailerAccess(block, mifareCard.Data);
                        Console.WriteLine($"Bloc: {block}, Access: {sector}");
                    }
                }
                else
                {
                    Console.WriteLine($"Autentication error");

                }
            }
        }

        private static void TestI2c()
        {
            var ftI2c = new Ft4222I2c(new I2cConnectionSettings(0, Bno055Sensor.DefaultI2cAddress));

            var bno055Sensor = new Bno055Sensor(ftI2c);

            Console.WriteLine($"Id: {bno055Sensor.Info.ChipId}, AccId: {bno055Sensor.Info.AcceleratorId}, GyroId: {bno055Sensor.Info.GyroscopeId}, MagId: {bno055Sensor.Info.MagnetometerId}");
            Console.WriteLine($"Firmware version: {bno055Sensor.Info.FirmwareVersion}, Bootloader: {bno055Sensor.Info.BootloaderVersion}");
            Console.WriteLine($"Temperature source: {bno055Sensor.TemperatureSource}, Operation mode: {bno055Sensor.OperationMode}, Units: {bno055Sensor.Units}");
            Console.WriteLine($"Powermode: {bno055Sensor.PowerMode}");
        }

        private static void TestSpi()
        {
            var ftSpi = new Ft4222Spi(new SpiConnectionSettings(0, 1) { ClockFrequency = 1_000_000, Mode = SpiMode.Mode0 });

            while (!Console.KeyAvailable)
            {
                ftSpi.WriteByte(0xFF);
                Thread.Sleep(500);
                ftSpi.WriteByte(0x00);
                Thread.Sleep(500);
            }
        }

        public static void TestGpio()
        {
            const int Gpio2 = 2;
            var gpioController = new GpioController(PinNumberingScheme.Board, new Ft4222Gpio());

            // Opening GPIO2
            gpioController.OpenPin(Gpio2);
            gpioController.SetPinMode(Gpio2, PinMode.Output);

            Console.WriteLine("Blinking GPIO2");
            while (!Console.KeyAvailable)
            {
                gpioController.Write(Gpio2, PinValue.High);
                Thread.Sleep(500);
                gpioController.Write(Gpio2, PinValue.Low);
                Thread.Sleep(500);
            }

            Console.ReadKey();
            Console.WriteLine("Reading GPIO2 state");
            gpioController.SetPinMode(Gpio2, PinMode.Input);
            while (!Console.KeyAvailable)
            {
                Console.Write($"State: {gpioController.Read(Gpio2)} ");
                Console.CursorLeft = 0;
                Thread.Sleep(50);
            }
        }

        public static void TestEvents()
        {
            const int Gpio2 = 2;
            var gpioController = new GpioController(PinNumberingScheme.Board, new Ft4222Gpio());

            // Opening GPIO2
            gpioController.OpenPin(Gpio2);
            gpioController.SetPinMode(Gpio2, PinMode.Input);

            Console.WriteLine("Setting up events on GPIO2 for rising and failing");

            gpioController.RegisterCallbackForPinValueChangedEvent(Gpio2, PinEventTypes.Falling | PinEventTypes.Rising, myCallbackFailing);

            Console.WriteLine("Event setup, press a key to remove the failing event");
            while (!Console.KeyAvailable)
            {
                var res = gpioController.WaitForEvent(Gpio2, PinEventTypes.Falling, new TimeSpan(0, 0, 0, 0, 50));
                if ((!res.TimedOut) && (res.EventTypes != PinEventTypes.None))
                {
                    myCallbackFailing(gpioController, new PinValueChangedEventArgs(res.EventTypes, Gpio2));
                }
                res = gpioController.WaitForEvent(Gpio2, PinEventTypes.Rising, new TimeSpan(0, 0, 0, 0, 50));
                if ((!res.TimedOut) && (res.EventTypes != PinEventTypes.None))
                {
                    myCallbackFailing(gpioController, new PinValueChangedEventArgs(res.EventTypes, Gpio2));
                }
            }
            Console.ReadKey();
            gpioController.UnregisterCallbackForPinValueChangedEvent(Gpio2, myCallbackFailing);
            gpioController.RegisterCallbackForPinValueChangedEvent(Gpio2, PinEventTypes.Rising, myCallback);

            Console.WriteLine("Event removed, press a key to remove all events and quit");
            while (!Console.KeyAvailable)
            {
                var res = gpioController.WaitForEvent(Gpio2, PinEventTypes.Rising, new TimeSpan(0, 0, 0, 0, 50));
                if ((!res.TimedOut) && (res.EventTypes != PinEventTypes.None))
                {
                    myCallback(gpioController, new PinValueChangedEventArgs(res.EventTypes, Gpio2));
                }
            }

            gpioController.UnregisterCallbackForPinValueChangedEvent(Gpio2, myCallback);
        }

        private static void myCallback(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            Console.WriteLine($"Event on GPIO {pinValueChangedEventArgs.PinNumber}, event type: {pinValueChangedEventArgs.ChangeType}");
        }

        private static void myCallbackFailing(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            Console.WriteLine($"Event on GPIO {pinValueChangedEventArgs.PinNumber}, event type: {pinValueChangedEventArgs.ChangeType}");
        }
    }
}
