// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Iot.Device.Card;
using Iot.Device.Card.CreditCardProcessing;
using Iot.Device.Card.Mifare;
using Iot.Device.Pn532;
using Iot.Device.Pn532.AsTarget;
using Iot.Device.Pn532.ListPassive;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Spi;
using System.Linq;
using System.Threading;

namespace DemoGrove
{
    class Program
    {

        static Dictionary<ushort, byte[]> PdolCodes = new Dictionary<ushort, byte[]>()
        {
               {0x9F59, new byte[3] {0xC8,0x80,0x00} }, // Terminal Transaction Information
               {0x9F5A, new byte[1] {0x00}}, // Terminal transaction Type. 0 = payment, 1 = withdraw
               {0x9F58, new byte[1] {0x01}}, // Merchant Type Indicator
               {0x9F66, new byte[4] {0xB6,0x20,0xC0,0x00}}, // Terminal Transaction Qualifiers
               {0x9F02, new byte[6] {0x00,0x00,0x10,0x00,0x00,0x00}}, // amount, authorized
               {0x9F03, new byte[6] {0x00,0x00,0x00,0x00,0x00,0x00}}, // Amount, Other 
               {0x9F1A, new byte[2] {0x01,0x24}}, // Terminal country code
               {0x5F2A, new byte[2] {0x01,0x24}}, // Transaction currency code
               {0x95, new byte[5] {0x00,0x00,0x00,0x00,0x00}}, // Terminal Verification Results
               {0x9A, new byte[3] {0x15,0x01,0x01}}, // Transaction Date
               {0x9C, new byte[1] {0x00}}, // Transaction Type
               {0x9F37, new byte[4] {0x82,0x3D,0xDE,0x7A} } // Unpredictable number
        };

//        static void Main(string[] args)
//        {
//            //Console.WriteLine("Attached debugger and press a key");
//            //while (!Console.KeyAvailable)
//            //    ;
//            //Console.ReadKey();

//            Pn532 pn532 = null;
//            if (args?.Length > 0)
//            {
//                if (args[0] == "-c")
//                {
//                    string device = "/dev/ttyS0";
//                    pn532 = new Pn532(device);
//                }
//                else if (args[0] == "-i")
//                {
//                    I2cConnectionSettings connectionString = new I2cConnectionSettings(1, Pn532.I2cDefaultAddress);
//                    var device = I2cDevice.Create(connectionString);
//                    pn532 = new Pn532(device);
//                }
//                else if (args[0] == "-s")
//                {
//                    var settings = new SpiConnectionSettings(0, 0)
//                    {
//                        ClockFrequency = 2_000_000,
//                        Mode = SpiMode.Mode0,
//                        ChipSelectLineActiveState = PinValue.Low,
//                        //    DataFlow = DataFlow.LsbFirst
//                    };
//                    SpiDevice device = SpiDevice.Create(settings);
//                    pn532 = new Pn532(device);
//                }

//            }
//            else
//            {
//                string device = "COM7";
//                //pn532 = new Pn532(device, LogLevel.Debug);
//                pn532 = new Pn532(device, LogLevel.None);
//            }

//#if DEBUG
//            //pn532.LogLevel = LogLevel.Debug;
//#endif
//            var version = pn532.FirmwareVersion;
//            if (version != null)
//            {
//                Console.WriteLine($"Is it a PN532!: {version.IsPn532}, Version: {version.Version}, Version supported: {version.VersionSupported}");
//                //pn532.SetSerialBaudRate(BaudRate.B0921600);

//                //DumpAllRegisters(pn532);
//                //ReadMiFare(pn532);
//                //FixRegister(pn532);
//                //pn532.SetAnalog106kbpsTypeA(new Analog106kbpsTypeAMode() { CIU_DemodWhenRfOn = 0x4D,  CIU_TxBitPhase = 0x83 });
//                //pn532.SetAnalogTypeB(new AnalogSettingsTypeBMode() { CIU_GsNOn = 0xFF, CIU_RxThreshold = 0x85, CIU_ModGsP = 0x10});

//                ReadCreditCard(pn532);
//                //ReadTypeB(pn532);
//                //TestMiFareAccess(pn532);
//                //AsTarget(pn532);
//            }
//            else
//                Console.WriteLine($"Error");

//        }

        static void AsTarget(Pn532 pn532)
        {
            byte[] retData = null;
            TargetModeInitialized modeInitialized = null;
            while ((!Console.KeyAvailable))
            {
                (modeInitialized, retData) = pn532.InitAsTarget(
                    TargetModeInitialization.PiccOnly, 
                    new TargetMifareParameters() { Atqa = new byte[] { 0x08, 0x00 }, Sak = 0x60 },
                    new TargetFeliCaParameters() { NfcId2 = new byte[] { 0x01, 0xFE, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7 }, Pad = new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7 } },
                    new TargetPiccParameters() { NfcId3 = new byte[] { 0xAA, 0x99, 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11 }, GeneralTarget = new byte[0], HistoricalTarget = new byte[0] });
                if (modeInitialized != null)
                    break;
                // Give time to PN532 to process
                Thread.Sleep(200);
            }
            if (modeInitialized == null)
                return;

            Console.WriteLine($"PN532 as a target: ISDep: {modeInitialized.IsDep}, IsPicc {modeInitialized.IsISO14443_4Picc}, {modeInitialized.TargetBaudRate}, {modeInitialized.TargetFramingType}");
            Console.WriteLine($"Initiator: {BitConverter.ToString(retData)}");
            // 25-D4-00-E8-11-6A-0A-69-1C-46-5D-2D-7C-00-00-00-32-46-66-6D-01-01-12-02-02-07-FF-03-02-00-13-04-01-64-07-01-03
            // 11-D4-00-01-FE-A2-A3-A4-A5-A6-A7-00-00-00-00-00-30            
            // E0-80

            Span<byte> read = stackalloc byte[512];
            int ret = -1;
            while (ret<0)
                ret = pn532.ReadDataAsTarget(read);

            // For example: 00-00-A4-04-00-0E-32-50-41-59-2E-53-59-53-2E-44-44-46-30-31-00
            Console.WriteLine($"Status: {read[0]}, Data: {BitConverter.ToString(read.Slice(1).ToArray())}");            
        }


        static void TestMiFareAccess(Pn532 pn532)
        {
            Span<byte> blockScetor = stackalloc byte[16] {
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0xFF,
                0x07,
                0x80,
                0x40,
                0xFF,
                0xFF,
                0xFF,
                0xFF,
                0xFF,
                0xFF
            };
            var mifare = new MifareCard(pn532, 1);
            var access = mifare.SectorTailerAccess(7, blockScetor.ToArray());
            var block0 = mifare.BlockAccess(4, blockScetor.ToArray());
            var block1 = mifare.BlockAccess(5, blockScetor.ToArray());
            var block2 = mifare.BlockAccess(6, blockScetor.ToArray());
            Console.WriteLine($"Access: {access}");
            Console.WriteLine($"Block0: {block0}");
            Console.WriteLine($"Block1: {block1}");
            Console.WriteLine($"Block2: {block2}");
            //Encode the access
            var encodedAccess = mifare.EncodeSectorTailer(access);
            var encodedBlock0 = mifare.EncodeSectorTailer(4, block0);
            var encodedBlock1 = mifare.EncodeSectorTailer(5, block1);
            var encodedBlock2 = mifare.EncodeSectorTailer(6, block2);
            byte encoded6 = (byte)(encodedAccess.Item1 | encodedBlock0.Item1 | encodedBlock1.Item1 | encodedBlock2.Item1);
            byte encoded7 = (byte)(encodedAccess.Item2 | encodedBlock0.Item2 | encodedBlock1.Item2 | encodedBlock2.Item2);
            byte encoded8 = (byte)(encodedAccess.Item3 | encodedBlock0.Item3 | encodedBlock1.Item3 | encodedBlock2.Item3);
            // Full function
            var encodedFull = mifare.EncodeSectorAndClockTailer(access, new AccessType[] { block0, block1, block2 });
            Console.WriteLine($"Original: {blockScetor[6].ToString("X2")} vs encoded: {encoded6.ToString("X2")} vs full: {encodedFull.Item1.ToString("X2")}");
            Console.WriteLine($"Original: {blockScetor[7].ToString("X2")} vs encoded: {encoded7.ToString("X2")} vs full: {encodedFull.Item2.ToString("X2")}");
            Console.WriteLine($"Original: {blockScetor[8].ToString("X2")} vs encoded: {encoded8.ToString("X2")} vs full: {encodedFull.Item3.ToString("X2")}");

        }

        static void DumpAllRegisters(Pn532 pn532)
        {
            const int MaxRead = 16;
            Span<byte> span = stackalloc byte[MaxRead];
            for (int i = 0; i < 0xFFFF; i += MaxRead)
            {
                ushort[] reg = new ushort[MaxRead];
                for (int j = 0; j < MaxRead; j++)
                    reg[j] = (ushort)(i + j);
                var ret = pn532.ReadRegister(reg, span);
                if (ret)
                {
                    Console.Write($"Reg: {(i).ToString("X4")} ");
                    for (int j = 0; j < MaxRead; j++)
                        Console.Write($"{span[j].ToString("X2")} ");
                    Console.WriteLine();
                }
            }
        }

        static void FixRegister(Pn532 pn532)
        {
            const int MaxRead = 16;
            Span<byte> span;
            ushort[] reg = new ushort[MaxRead];
            // 03E0 9F CB 3C C0 5B 00 3B 8E CE 51 00 00 02 85 08 3E
            for (ushort i = 0; i < MaxRead; i++)
                reg[i] = (ushort)(0x03E0 + i);
            span = (new byte[16] { 0x9F, 0xCB, 0x3C, 0xC0, 0x5B, 0x00, 0x3B, 0x8E, 0xCE, 0x51, 0x00, 0x00, 0x02, 0x85, 0x08, 0x3E });
            var ret = pn532.WriteRegister(reg, span);
            if (ret)
                Console.WriteLine($"1 reg ok");
            // 03F0 81 3E 81 3E 81 00 00 00 00 00 00 00 00 00 00 AC
            for (ushort i = 0; i < MaxRead; i++)
                reg[i] = (ushort)(0x03F0 + i);
            span = (new byte[16] { 0x81, 0x3E, 0x81, 0x3E, 0x81, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xAC });
            ret = pn532.WriteRegister(reg, span);
            if (ret)
                Console.WriteLine($"2 reg ok");
            //  07E0 9F CB 3C C0 5B 51 00 00 02 85 08 3E 81 3E 81 3E
            for (ushort i = 0; i < MaxRead; i++)
                reg[i] = (ushort)(0x07E0 + i);
            span = (new byte[16] { 0x9F, 0xCB, 0x3C, 0xC0, 0x5B, 0x51, 0x00, 0x00, 0x02, 0x85, 0x08, 0x3E, 0x81, 0x3E, 0x81, 0x3E });
            ret = pn532.WriteRegister(reg, span);
            if (ret)
                Console.WriteLine($"3 reg ok");
            // 07F0 81 3E 81 3E 81 00 00 00 00 00 00 00 00 00 00 AC
            for (ushort i = 0; i < MaxRead; i++)
                reg[i] = (ushort)(0x07F0 + i);
            span = (new byte[16] { 0x81, 0x3E, 0x81, 0x3E, 0x81, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xAC });
            ret = pn532.WriteRegister(reg, span);
            if (ret)
                Console.WriteLine($"4 reg ok");
            // 6100 00 00 00 04 00 20 43 00 00 04 00 00 00 01 00 00
            for (ushort i = 0; i < MaxRead; i++)
                reg[i] = (ushort)(0x6100 + i);
            span = (new byte[16] { 0x00, 0x00, 0x00, 0x04, 0x00, 0x20, 0x43, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00 });
            ret = pn532.WriteRegister(reg, span);
            if (ret)
                Console.WriteLine($"5 reg ok");
            // 6320 00 00 00 00 00 80 40 80 00 00 03 88 FF 00 03 00
            for (ushort i = 0; i < MaxRead; i++)
                reg[i] = (ushort)(0x6320 + i);
            span = (new byte[16] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x40, 0x80, 0x00, 0x00, 0x03, 0x88, 0xFF, 0x00, 0x03, 0x00 });
            ret = pn532.WriteRegister(reg, span);
            if (ret)
                Console.WriteLine($"6 reg ok");
            // 80E0 9F CB 3C C0 5B 51 00 00 02 85 08 3E 81 3E 81 3E
            for (ushort i = 0; i < MaxRead; i++)
                reg[i] = (ushort)(0x80E0 + i);
            span = (new byte[16] { 0x9F, 0xCB, 0x3C, 0xC0, 0x5B, 0x51, 0x00, 0x00, 0x02, 0x85, 0x08, 0x3E, 0x81, 0x3E, 0x81, 0x3E });
            ret = pn532.WriteRegister(reg, span);
            if (ret)
                Console.WriteLine($"7 reg ok");
            //  80F0 81 3E 81 3E 81 00 00 00 00 00 00 00 00 00 00 AC
            for (ushort i = 0; i < MaxRead; i++)
                reg[i] = (ushort)(0x80F0 + i);
            span = (new byte[16] { 0x81, 0x3E, 0x81, 0x3E, 0x81, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xAC });
            ret = pn532.WriteRegister(reg, span);
            if (ret)
                Console.WriteLine($"8 reg ok");


        }

        static void ReadTypeB(Pn532 pn532)
        {
            //ushort[] Regs = new ushort[11] { 0x633C, 0x6302, 0x6303, 0x6308, 0x6309, 0x6316, 0x6317, 0x6318, 0x6319, 0x630E, 0x6305 };
            //byte[] toWrite = new byte[11] { 0x10, 0x83, 0x83, 0x85, 0x4D, 0x69, 0xFF, 0x3F, 0x10, 0x00, 0x07 };
            //pn532.WriteRegister(Regs, toWrite);
            //pn532.WriteRegister(new ushort[] { 0x6305 }, new byte[] { 0x0C });
            //Regs = new ushort[4] { 0x633A, 0x6339, 0x6339, 0x6339 };
            //toWrite = new byte[4] { 0x80, 0x05, 0x00, 0x00 };
            //pn532.WriteRegister(Regs, toWrite);
            //Span<byte> span = stackalloc byte[1];
            //bool ret;

            //ret = pn532.ReadRegister(new ushort[] { 0x633A }, span);
            //Console.WriteLine($"Read Fifo: {ret}, {BitConverter.ToString(span.ToArray())}");

            //pn532.WriteRegister(new ushort[] { 0x633D }, new byte[] { 0x80 });
            //span = (new byte[16]).AsSpan();
            //Regs = new ushort[16] { 0x6334, 0x6336, 0x633a, 0x6339, 0x6339, 0x6339, 0x6339, 0x6339, 0x6339, 0x6339, 0x6339, 0x6339, 0x6339, 0x6339, 0x6339, 0x633A };
            //ret = pn532.ReadRegister(Regs, span);
            //Console.WriteLine($"Read ATQB: {ret}, {BitConverter.ToString(span.ToArray())}");
            // Soft reset
            pn532.WriteRegister(0x6331, 0x0F);
            // IC Configuration
            pn532.WriteRegister(0x633C, 0x10);
            pn532.WriteRegister(0x6302, 0x83);
            pn532.WriteRegister(0x6303, 0x83);
            pn532.WriteRegister(0x6308, 0x85);
            pn532.WriteRegister(0x6309, 0x4D);
            pn532.WriteRegister(0x6316, 0x69);
            pn532.WriteRegister(0x6317, 0xFF);
            pn532.WriteRegister(0x6318, 0x3F);
            pn532.WriteRegister(0x6319, 0x10);
            pn532.WriteRegister(0x630E, 0x00);
            pn532.WriteRegister(0x6305, 0x07);
            //Start transceive
            pn532.WriteRegister(0x6331, 0x0C);
            // REQB/WUPB
            pn532.WriteRegister(0x633A, 0x80);
            pn532.WriteRegister(0x6339, 0x05);
            pn532.WriteRegister(0x6339, 0x00);
            pn532.WriteRegister(0x6339, 0x00);
            // ReadFIFOLEvel
            byte fifoLevel = 0;
            var ret = pn532.ReadRegister(0x633A, out fifoLevel);
            Console.WriteLine($"Fifo level: {ret}, {fifoLevel.ToString("X2")}");
            pn532.WriteRegister(0x633D, 0x80);
            fifoLevel = 0;
            ret = pn532.ReadRegister(0x6334, out fifoLevel);
            Console.WriteLine($"CommIrqReq: {ret}, {fifoLevel.ToString("X2")}");
            ret = pn532.ReadRegister(0x6336, out fifoLevel);
            Console.WriteLine($"ErrReg: {ret}, {fifoLevel.ToString("X2")}");
            ret = pn532.ReadRegister(0x633A, out fifoLevel);
            Console.WriteLine($"ErrReg: {ret}, {fifoLevel.ToString("X2")}");

        }


        static void ReadCreditCard(Pn532 pn532)
        {
            byte[] retData = null;
            while ((!Console.KeyAvailable))
            {
                retData = pn532.AutoPoll(5, 300, new PollingType[] { PollingType.Passive106kbpsISO144443_4B });
                if (retData != null)
                {
                    if (retData.Length >= 3)
                        break;
                }

                // Give time to PN532 to process
                Thread.Sleep(200);
            }

            if (retData == null)
                return;

            //Check how many tags and the type
            Console.WriteLine($"Num tags: {retData[0]}, Type: {(PollingType)retData[1]}");
            var decrypted = pn532.TryDecodeData106kbpsTypeB(retData.AsSpan().Slice(3));
            if (decrypted != null)
            {
                Console.WriteLine($"{decrypted.TargetNumber}, Serial: {BitConverter.ToString(decrypted.NfcId)}, App Data: {BitConverter.ToString(decrypted.ApplicationData)}, " +
                    $"{decrypted.ApplicationType}, Bit Rates: {decrypted.BitRates}, CID {decrypted.CidSupported}, Command: {decrypted.Command}, FWT: {decrypted.FrameWaitingTime}, " +
                    $"ISO144443 compliance: {decrypted.ISO14443_4Compliance}, Max Frame size: {decrypted.MaxFrameSize}, NAD: {decrypted.NadSupported}");

                CreditCard creditCard = new CreditCard(pn532, decrypted.TargetNumber);
                creditCard.ReadCreditCardInformation();

                Console.WriteLine("All Tags for the Credit Card:");
                DisplayTags(creditCard.Tags, 0);

            }
        }

        static string AddSpace(int level)
        {
            string space = "";
            for (int i = 0; i < level; i++)
                space += "  ";

            return space;
        }

        static void DisplayTags(List<Tag> tagToDisplay, int levels)
        {
            foreach (var tagparent in tagToDisplay)
            {
                Console.Write(AddSpace(levels) + $"{tagparent.TagNumber.ToString("X4")}-{TagList.Tags.Where(m => m.TagNumber == tagparent.TagNumber).FirstOrDefault()?.Description}");
                var isTemplate = TagList.Tags.Where(m => m.TagNumber == tagparent.TagNumber).FirstOrDefault();
                if ((isTemplate?.IsTemplate == true) || (isTemplate?.IsConstructed == true))
                {
                    Console.WriteLine();
                    DisplayTags(tagparent.Tags, levels + 1);
                }
                else if (isTemplate?.IsDol == true)
                {
                    //In this case, all the data inside are 1 byte only
                    Console.WriteLine(", Data Object Length elements:");
                    foreach (var dt in tagparent.Tags)
                    {
                        Console.Write(AddSpace(levels + 1) + $"{dt.TagNumber.ToString("X4")}-{TagList.Tags.Where(m => m.TagNumber == dt.TagNumber).FirstOrDefault()?.Description}");
                        Console.WriteLine($", data length: {dt.Data[0]}");
                    }
                }
                else
                {
                    TagDetails tg = new TagDetails(tagparent);
                    Console.WriteLine($": {tg.ToString()}");
                }
            }
        }

        static void TestCCDirect(Pn532 pn532, byte Tg)
        {
            var toSend = new byte[] { 0x00,0xA4,0x04,0x00, // CLA - INS - P1 - P2
                0x0e, // Length
                0x32,0x50,0x41,0x59,0x2e,0x53,0x59,0x53,0x2e,0x44,0x44,0x46,0x30,0x31, // 1PAY.SYS.DDF01 (PPSE)
                0x00};
            Span<byte> span = stackalloc byte[260];
            var ret = pn532.Transceive(Tg, toSend, span);
            //if (ret >= 0)
            //{
            //    //Console.WriteLine($"{BitConverter.ToString(span.ToArray())}");
            //    FileControlInformation fs = new FileControlInformation(span.Slice(0, ret));
            //    Console.WriteLine($"Dedicated Name: {BitConverter.ToString(fs.DedicatedFileName)}");
            //    foreach (var app in fs.ProprietaryTemplate.ApplicationTemplates)
            //    {
            //        Console.WriteLine($"APPID: {BitConverter.ToString(app.ApplicationIdentifier)}, Priority: {app.ApplicationPriorityIndicator}, {app.ApplicationLabel}");
            //        //var test = new byte[] { 0x00, 0xB2, 0x01, 0x0C, 0x00 };
            //        // This is working and give some data:
            //        //var test = new byte[] { 0x00, 0xA4, 0x04, 0x00, 0x07, 0xA0, 0x00, 0x00, 0x00, 0x42, 0x10, 0x10, 0x00 };
            //        var tests = new byte[6 + app.ApplicationIdentifier.Length];
            //        tests[0] = 0x00;
            //        tests[1] = 0xA4;
            //        tests[2] = 0x04;
            //        tests[3] = 0x00;
            //        tests[4] = (byte)app.ApplicationIdentifier.Length;
            //        app.ApplicationIdentifier.CopyTo(tests, 5);
            //        tests[5 + app.ApplicationIdentifier.Length] = 0x00;

            //        ret = pn532.WriteRead(Tg, tests, span);
            //        if (ret >= 0)
            //        {
            //            //Console.WriteLine($"{BitConverter.ToString(span.ToArray())}");
            //            // AddEntryPaylog(pn532, span, decrypted.Tg)
            //            var recordInfos = new byte[] {0x00,0xB2, // READ RECORD
            //     0x00,0x00, // count + (SFI/flags),  edited dynamically
            //     0x00};

            //            for (int sfi = 0; sfi <= 2; sfi++)
            //            {

            //                recordInfos[3] = (byte)((sfi << 3) | (1 << 2));
            //                for (byte record = 0; record <= 2; record++)
            //                {
            //                    recordInfos[2] = record;

            //                    ret = pn532.WriteRead(Tg, recordInfos, span);
            //                    if (ret >= 0)
            //                    {
            //                        try
            //                        {
            //                            var Emv = new EmvProprietaryTemplate(span.Slice(0, ret));
            //                            Console.WriteLine($"{BitConverter.ToString(span.ToArray())}");
            //                            Console.WriteLine($"Name: {Emv.CardHolder}, Extended Name: {Emv.CardHolderExtended}");
            //                            if (Emv.Track1DiscretionaryData != null)
            //                                Console.WriteLine($"Track1: { BitConverter.ToString(Emv.Track1DiscretionaryData)}");
            //                        }
            //                        catch (Exception)
            //                        {
            //                            Console.WriteLine($"{BitConverter.ToString(span.ToArray())}");
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        //var gpoheaders = new byte[6 + app.ApplicationIdentifier.Length];
            //        //gpoheaders[0] = 0x00;
            //        //gpoheaders[1] = 0xA4;
            //        //gpoheaders[2] = 0x04;
            //        //gpoheaders[3] = 0x00;
            //        //gpoheaders[4] = (byte)app.ApplicationIdentifier.Length;
            //        //app.ApplicationIdentifier.CopyTo(gpoheaders, 4);
            //        //gpoheaders[4 + app.ApplicationIdentifier.Length] = 0x00;
            //        ////gpoheaders[4 + app.ApplicationIdentifier.Length + 1] = 0x90;
            //        ////gpoheaders[4 + app.ApplicationIdentifier.Length + 2] = 0x00;

            //        //ret = pn532.InDataExchange(decrypted.Tg, gpoheaders, span);
            //        //if (ret)
            //        //{
            //        //    Console.WriteLine($"{BitConverter.ToString(span.ToArray())}");
            //        //    for (byte sfi = 1; sfi <= 31; sfi++)
            //        //    {
            //        //        for (byte rec = 1; rec <= 16; rec++)
            //        //        {
            //        //            var details = new byte[5] { 0x00, 0xB2, rec, (byte)((sfi << 3) | 4), 0x00 };
            //        //            ret = pn532.InDataExchange(decrypted.Tg, details, span);
            //        //            //var tlv = card.sendApdu(0x00, 0xB2, rec, (sfi << 3) | 4, 0x00);
            //        //            if (ret)
            //        //            {
            //        //                Console.WriteLine($"{BitConverter.ToString(span.ToArray())}");
            //        //                //if (card.SW == 0x9000)
            //        //                //{
            //        //                //    print("SFI " + sfi.toString(16) + " record #" + rec);
            //        //                //    try
            //        //                //    {
            //        //                //        var asn = new ASN1(tlv);
            //        //                //        print(asn);
            //        //                //    }
            //        //                //    catch (e)
            //        //                //    {
            //        //                //        print(tlv.toString(HEX));
            //        //                //    }
            //        //                //}
            //        //            }
            //        //        }
            //        //    }

            //        //}
            //    }
            //    //          var gpoheaders = new byte[] {   0x80,0xCA, // GET DATA
            //    //0x9F,0x4F, // 9F4F asks for the log format
            //    //0x00};

            //}

        }

        static void AddEntryPaylog(Pn532 pn532, Span<byte> span, byte Tg)
        {
            //    var details = new FileControlInformation(span)?.ProprietaryTemplate;
            //    if (details != null)
            //    {
            //        Console.WriteLine($"Prefered language: {details.LanguagePreference}, Card type: {details.ApplicationLabel}, Priority: {details.ApplicationPriorityIndicator}");
            //        Console.WriteLine($"PDOL: {BitConverter.ToString(details.ProcessingOptionsDataObjectList)}");
            //        byte sizePdolCodes = 0;
            //        foreach (var elem in details.ProcessOptionsDecoded)
            //        {
            //            Console.WriteLine($"Tag: {elem.TagNumber.ToString("X4")}, Data:{elem.Data[0]}");
            //            sizePdolCodes += elem.Data[0];
            //        }
            //        var podlDetails = new byte[4 + 3 + sizePdolCodes + 1]; // {0x40,0x01, 0x80,0xA8,0x00,0x00};
            //        int index = 0;
            //        podlDetails[index++] = 0x80;
            //        podlDetails[index++] = 0xA8;
            //        podlDetails[index++] = 0x00;
            //        podlDetails[index++] = 0x00;
            //        podlDetails[index++] = (byte)(sizePdolCodes + 2);
            //        podlDetails[index++] = 0x83;
            //        podlDetails[index++] = sizePdolCodes;
            //        foreach (var elem in details.ProcessOptionsDecoded)
            //        {
            //            PdolCodes[elem.TagNumber].CopyTo(podlDetails, index);
            //            index += elem.Data[0];
            //        }
            //        podlDetails[index] = 0x00;
            //        var ret = pn532.WriteRead(Tg, podlDetails, span);
            //        if (ret >= 0)
            //        {
            //            Console.WriteLine($"{BitConverter.ToString(span.ToArray())}");
            //        }
            //    }
        }

        static void ReadMiFare(Pn532 pn532)
        {
            byte[] retData = null;
            while ((!Console.KeyAvailable))
            {
                retData = pn532.ListPassiveTarget(MaxTarget.One, TargetBaudRate.B106kbpsTypeA);
                //retData = pn532.AutoPoll(2, 300, new PollingType[] { PollingType.DepActive106kbps, PollingType.DepPassive106kbps, PollingType.GenericPassive106kbps, PollingType.InnovisionJewel, PollingType.MifareCard, PollingType.Passive106kbps, PollingType.Passive106kbpsISO144443_4A, PollingType.Passive106kbpsISO144443_4B });
                if (retData != null)
                    break;
                // Give time to PN532 to process
                Thread.Sleep(200);
            }
            if (retData == null)
                return;
            var decrypted = pn532.TryDecode106kbpsTypeA(retData.AsSpan().Slice(1));
            if (decrypted != null)
            {
                Console.WriteLine($"Tg: {decrypted.TargetNumber}, ATQA: {decrypted.Atqa} SAK: {decrypted.Sak}, NFCID: {BitConverter.ToString(decrypted.NfcId)}");
                if (decrypted.Ats != null)
                    Console.WriteLine($", ATS: {BitConverter.ToString(decrypted.Ats)}");
                MifareCard mifareCard = new MifareCard(pn532, decrypted.TargetNumber) { BlockNumber = 0, Command = MifareCardCommand.AuthenticationA };
                mifareCard.SetCapacity(decrypted.Atqa, decrypted.Sak);
                mifareCard.SerialNumber = decrypted.NfcId;
                mifareCard.KeyA = new byte[6] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                mifareCard.KeyB = new byte[6] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };//new byte[6] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                for (byte block = 0; block < 64; block++)
                {
                    mifareCard.BlockNumber = block;
                    mifareCard.Command = MifareCardCommand.AuthenticationB;
                    var ret = mifareCard.RunMifiCardCommand();
                    if (ret < 0)
                    {
                        // Try another one
                        mifareCard.Command = MifareCardCommand.AuthenticationA;
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
                            Console.WriteLine($"Error reading bloc: {block}, Data: {BitConverter.ToString(mifareCard.Data)}");
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

                //ChangePermissions(pn532, mifareCard);

                //Console.WriteLine("press a key to exit");
                //while (!Console.KeyAvailable)
                //    ;
            }
        }

        static void ChangePermissions(Pn532 pn532, MifareCard mifareCard)
        {
            // keep permsissions of sector 0-3 and change the one from sector 4-63
            // Change the key B to default FF FF FF FF FF FF
            var newKey = new byte[6] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            for (byte i = 7; i < 64; i += 4)
            {
                mifareCard.BlockNumber = i;
                mifareCard.Command = MifareCardCommand.AuthenticationA;
                var ret = mifareCard.RunMifiCardCommand();
                if (ret >= 0)
                {
                    mifareCard.BlockNumber = i;
                    mifareCard.Command = MifareCardCommand.Read16Bytes;
                    ret = mifareCard.RunMifiCardCommand();
                    if (ret >= 0)
                    {
                        //var access = AccessType.ReadKeyA | AccessType.ReadKeyB | AccessType.WriteKeyA | AccessType.WriteKeyB | AccessType.IncrementKeyA | AccessType.IncrementKeyB | AccessType.DecrementTransferRestoreKeyA | AccessType.DecrementTransferRestoreKeyB;
                        //var sector = AccessSector.KeyAWriteKeyA | AccessSector.AccessBitsReadKeyA | AccessSector.KeyBWriteKeyA | AccessSector.KeyBRead | AccessSector.KeyBReadKeyA;
                        //var accessNew = new AccessType[3] { access, access, access };
                        //var encoded = mifareCard.EncodeSectorAndClockTailer(sector, accessNew);
                        //Console.WriteLine($"{encoded.Item1}, {encoded.Item2}, {encoded.Item3}");
                        mifareCard.Data[6] = 0xFF; //encoded.Item1;
                        mifareCard.Data[7] = 0x07; //encoded.Item2;
                        mifareCard.Data[8] = 0x80; // encoded.Item3;
                        mifareCard.Command = MifareCardCommand.Write16Bytes;
                        ret = mifareCard.RunMifiCardCommand();
                        if (ret >= 0)
                            Console.WriteLine("permossion changed");
                    }
                }
            }
        }


        //Console.WriteLine($"block scetor, changing Key");
        //blockData[15] = 0xFF;
        //blockData[14] = 0xFF;
        //blockData[13] = 0xFF;
        //blockData[12] = 0xFF;
        //blockData[11] = 0xFF;
        //blockData[10] = 0xFF;
        //mifareCard.Data = blockData.ToArray();
        //mifareCard.Command = MifareCardCommand.Write16Bytes;
        //ret = pn532.RunMifiCardCommand(mifareCard, blockData);
        //if (ret)
        //    Console.WriteLine($"success, {BitConverter.ToString(blockData.ToArray())}");

        static void RunTests(Pn532 pn532)
        {
            Console.WriteLine($"{DiagnoseMode.CommunicationLineTest}: {pn532.RunSelfTest(DiagnoseMode.CommunicationLineTest)}");
            Console.WriteLine($"{DiagnoseMode.ROMTest}: {pn532.RunSelfTest(DiagnoseMode.ROMTest)}");
            Console.WriteLine($"{DiagnoseMode.RAMTest}: {pn532.RunSelfTest(DiagnoseMode.RAMTest)}");
            // Check couple of SFR registers
            SfrRegister[] regs = new SfrRegister[] { SfrRegister.HSU_CNT, SfrRegister.HSU_CTR, SfrRegister.HSU_PRE, SfrRegister.HSU_STA };
            Span<byte> redSfrs = stackalloc byte[regs.Length];
            var ret = pn532.ReadRegisterSfr(regs, redSfrs);
            for (int i = 0; i < regs.Length; i++)
                Console.WriteLine($"Readregisters: {regs[i]}, value: {BitConverter.ToString(redSfrs.ToArray(), i, 1)} ");
            // This should give the same result as
            ushort[] regus = new ushort[] { 0xFFAE, 0xFFAC, 0xFFAD, 0xFFAB };
            Span<byte> redSfrus = stackalloc byte[regus.Length];
            ret = pn532.ReadRegister(regus, redSfrus);
            for (int i = 0; i < regus.Length; i++)
                Console.WriteLine($"Readregisters: {regus[i]}, value: {BitConverter.ToString(redSfrus.ToArray(), i, 1)} ");
            Console.WriteLine($"Are results same: {redSfrus.SequenceEqual(redSfrs)}");
            // Access GPIO
            ret = pn532.ReadGpio(out Port7 p7, out Port3 p3, out OperatingMode l0L1);
            Console.WriteLine($"P7: {p7}");
            Console.WriteLine($"P3: {p3}");
            Console.WriteLine($"L0L1: {l0L1} ");
        }


    }
}
