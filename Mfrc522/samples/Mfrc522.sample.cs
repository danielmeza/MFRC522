using Iot.Device.Card;
using Iot.Device.Card.Mifare;
using System;
using System.Device.Spi;
using System.Linq;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Iot.Device.Mfrc522
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello Mfrc522!");

            // This sample demonstrates how to read a RFID tag
            // using the Mfrc522Controller
            var connection = new SpiConnectionSettings(0, 0);
            connection.ClockFrequency = 500000;

            var spi = SpiDevice.Create(connection);
            using (var mfrc522Controller = new Mfrc522Controller(spi))
            {
                mfrc522Controller.LogLevel = LogLevel.Info;
                mfrc522Controller.LogTo = LogTo.Console;
                //await ReadCardUidLoop(mfrc522Controller);
                //mfrc522Controller.RxGain = RxGain.G48dB;
                ReadCardAuth(mfrc522Controller);
            }

            await Task.CompletedTask;
        }

        private static void ReadCardAuth(Mfrc522Controller mfrc522Controller)
        {
            //while (!Console.KeyAvailable)
            //{
            //    var (status, atqa) = mfrc522Controller.Request(RequestMode.RequestIdle);

            //    if (status == Status.OK)
            //    {
            //        Console.WriteLine($"Card detected, ATQA: {BitConverter.ToString(atqa)}");
            //        break;
            //    }
            //    // Don't overload the reader
            //    Thread.Sleep(100);
            //}
            //if (Console.KeyAvailable)
            //{
            //    Console.WriteLine("No card detected");
            //    return;
            //}


            //var (status2, cardUid) = mfrc522Controller.AntiCollision();
            //Console.WriteLine($"Card UID: {BitConverter.ToString(cardUid)}, Length: {cardUid.Length}");

            //var (stat, sak) = mfrc522Controller.SelectTag(cardUid);
            //Console.WriteLine($"SAK: {sak}");
            Console.WriteLine($"Version: {mfrc522Controller.Version}");
            byte[] buff = null;
            while (!Console.KeyAvailable)
            {
                buff = mfrc522Controller.ListPassiveTarget();
                if (buff != null)
                    break;
            }
            if (Console.KeyAvailable)
            {
                Console.WriteLine("No card detected");
                return;
            }


            var mifare = new MifareCard(mfrc522Controller, 1);
            mifare.SerialNumber = buff.AsSpan().Slice(3, 4).ToArray();
            mifare.KeyA = new byte[6] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            mifare.KeyB = new byte[6] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            mifare.Command = MifareCardCommand.AuthenticationA;
            int ret;
            //mifare.BlockNumber = 7;
            //ret = mifare.RunMifiCardCommand();
            //if (ret < 0)
            //{
            //    Console.WriteLine("Authentication failed, trying the other key");
            //    mifare.Command = MifareCardCommand.AuthenticationB;
            //    ret = mifare.RunMifiCardCommand();
            //    if (ret < 0)
            //    {
            //        Console.WriteLine("Authentification failed with second key");
            //    }
            //}
            //Console.WriteLine("Success authenticating!");
            
            for (byte block = 0; block < 64; block++)
            {
                mifare.BlockNumber = block;
                mifare.Command = MifareCardCommand.AuthenticationB;
                ret = mifare.RunMifiCardCommand();
                if (ret < 0)
                {
                    // Try another one
                    mifare.Command = MifareCardCommand.AuthenticationA;
                    ret = mifare.RunMifiCardCommand();
                }
                //var ret = pn532.InDataExchange(mifareCard.Tg, new byte[] { 0x60, block, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x66, 0x8D, 0x7E, 0x20 }, Span<byte>.Empty);
                if (ret >= 0)
                {
                    mifare.BlockNumber = block;
                    mifare.Command = MifareCardCommand.Read16Bytes;
                    ret = mifare.RunMifiCardCommand();
                    if (ret >= 0)
                        Console.WriteLine($"Bloc: {block}, Data: {BitConverter.ToString(mifare.Data)}");
                    else
                    {
                        Console.WriteLine($"Error reading bloc: {block}");
                    }
                    if (block % 4 == 3)
                    {
                        if (mifare.Data != null)
                        {
                            // Check what are the permissions
                            for (byte j = 3; j > 0; j--)
                            {

                                var access = mifare.BlockAccess((byte)(block - j), mifare.Data);
                                Console.WriteLine($"Bloc: {block - j}, Access: {access}");
                            }
                            var sector = mifare.SectorTailerAccess(block, mifare.Data);
                            Console.WriteLine($"Bloc: {block}, Access: {sector}");
                        }
                        else
                            Console.WriteLine("Can't check any sector bloc");
                    }
                }
                else
                {
                    Console.WriteLine($"Autentication error");

                }


                //if (mfrc522Controller.Authenticate(RequestMode.Authenticate1A, 7, mifare.KeyA, cardUid) == Status.OK)
                //{
                //    var data = new byte[16 * 3];
                //    for (var x = 0; x < data.Length; x++)
                //    {
                //        data[x] = (byte)(x + 65);
                //    }

                //    for (var b = 0; b < 3; b++)
                //    {
                //        //mfrc522Controller.WriteCardData((byte)(4 + b), data.Skip(b * 16).Take(16).ToArray());
                //    }
                //}

                //// Reading data
                //var continueReading = true;
                //for (var s = 0; s < 16 && continueReading; s++)
                //{
                //    // Authenticate sector
                //    if (mfrc522Controller.Authenticate(RequestMode.Authenticate1A, (byte)((4 * s) + 3), Mfrc522Controller.DefaultAuthKey, cardUid) == Status.OK)
                //    {
                //        Console.WriteLine($"Sector {s}");
                //        for (var b = 0; b < 3 && continueReading; b++)
                //        {
                //            byte[] data;
                //            (stat, data) = mfrc522Controller.ReadCardData((byte)((4 * s) + b));
                //            if (stat != Status.OK)
                //            {
                //                continueReading = false;
                //                break;
                //            }

                //            Console.WriteLine($"Block {b} ({data.Length} bytes): {string.Join(" ", data.Select(x => x.ToString("X2")))}");
                //        }
                //    }
                //    else
                //    {
                //        Console.WriteLine("Authentication error");
                //        break;
                //    }
                //}

                mfrc522Controller.ClearSelection();
            }
        }

        private static async Task ReadCardUidLoop(Mfrc522Controller mfrc522Controller)
        {
            while (true)
            {
                var (status, _) = mfrc522Controller.Request(RequestMode.RequestIdle);

                if (status != Status.OK)
                    continue;

                var (status2, uid) = mfrc522Controller.AntiCollision();
                Console.WriteLine(string.Join(", ", uid));

                await Task.Delay(500);
            }
        }
    }
}
