using Acrelec.Library.Logger;
using ECRUtilATLLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Acrelec.Mockingbird.Payment
{
    public class DoPrintJob
    {
        public static String isostring = "iso-8859-1";
       // PrintInformationClass printInformation = new PrintInformationClass();


        //TLV field definitions
        private static byte PROTOCOL_MESSAGE_SIZE = 4;
        private static byte PROTOCOL_VERSION_SIZE = 2;
        private static byte PROTOCOL_TLV_TAG_SIZE = 4;
        private static byte PROTOCOL_TLV_LENGTH_SIZE = 4;
        private static byte PROTOCOL_HEADER_SIZE = 6;
        private static byte PROTOCOL_BEGIN_DATA_TLV = 8;
        private static short DO_PRINTREQUEST_TAG = 0x2100;
        private static int PROTOCOL_VERSION = 0x0200; //2.00


        // Printing server Defns
        private Socket ServerSocket;
        private List<Socket> ClientSocketList = new List<Socket>();

        //public event EventHandler OnDoPrintJobOutput;
        public DoPrintJobPublisher _DoPrintJobPublisher = new DoPrintJobPublisher();
        public Byte DoPrintJobStatus;

        // CriticalSection is a variable used to make a semaphore for updating PrintJob text area
        private bool _CriticalPrintJobSection = false;

        // Printed lines
        private Int32 _DoPrintJobNbLine = 0;


        /// <summary>
        /// A terminal that does not have a printer device (For example IPP3xx) will send the data to print to
        /// the ECR through a TCP/IP socket.
        /// If the terminal includes a printer, the Print Job Service will be configurable from the terminal 
        /// setup menu(Setup  Printer  On/Off).
        /// The ECR application should then create a TCP/IP socket server in order to get any print information
        /// sent from the terminal.
        /// For each line to be printed, the terminal initiate a client socket, send the line data in a 
        /// micro-line buffer and then close the socket.
        /// </summary>
        public void DoThePrintJob()
        {
            const Int32 TAG_ECR_PRINT_JOB_STATUS = 0x00080016;
            short DO_PRINTRESPONSE_TAG = 0x2101;

          //  EventArgs e = new EventArgs();
            List<Socket> ResponseSocketList = new List<Socket>();

            // Socket Creation
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {

                Log.Info("Starting the do print job server - Opening Server Socket on port 5187");
                //Bind socket to IP end point
                ServerSocket.Bind(new IPEndPoint(IPAddress.Any, 5187));
                //Listen on chosen port
                ServerSocket.Listen(10);
            }
            catch
            {
                Log.Error("Error to start the do print job server.\nCheck that there is no instance of server opened");

                return;
            }

            try
            {
                while (true)
                {
                    List<Socket> CheckReadList = new List<Socket>();
                    List<Socket> CheckWriteList = new List<Socket>();
                    List<Socket> ReadyToCloseList = new List<Socket>();

                    CheckReadList.Add(ServerSocket);
                    CheckReadList.AddRange(ClientSocketList);

                    CheckWriteList.AddRange(ResponseSocketList);

                    Socket.Select(CheckReadList, CheckWriteList, null, Timeout.Infinite);

                    _DoPrintJobPublisher._PrintingFontStyle = new PrintingFontStyle();

                    //---- Process ready to read list
                    foreach (Socket sock in CheckReadList)
                    {
                        if (sock.Equals(ServerSocket))
                        {
                            //---- Accept the incoming connection
                            ClientSocketList.Add(ServerSocket.Accept());
                        }
                        else
                        {
                            //---- Process client request
                            byte[] header = new byte[8];
                            bool messageOK = false;

                            if (sock.Receive(header, header.Length, SocketFlags.None) == 8)
                            {
                                int messageLength = BitConverter.ToInt32(header, 0);
                                byte[] msg = new byte[8 + messageLength];

                                Array.Copy(header, 0, msg, 0, 8);

                                if (sock.Receive(msg, 8, messageLength, SocketFlags.None) == messageLength)
                                {
                                    int avail = msg.Length;
                                    byte[] rcvd = new byte[avail];
                                    byte[] Tag = new byte[4];
                                    byte[] Length = new byte[4];
                                    int counter = 0;
                                    int rcvdCounter = 0;

                                    messageOK = true;

                                    // Unpack TLV fields
                                    msg = TLVFieldUnpack(msg, avail, ref Tag, ref Length);

                                    if (BitConverter.ToInt32(Length, 0) != 0) // Is not a paper status check request
                                    {
                                        while (counter < avail)
                                        {
                                            if (msg[counter] == '\n')
                                            {
                                                if (rcvdCounter == 0)
                                                {
                                                    rcvd[rcvdCounter] = (byte)' ';
                                                }
                                                break;
                                            }
                                            else
                                            {
                                                // Decode microline commands
                                                if ((msg[counter] >= '\x0e') && (msg[counter] <= '\x1f'))
                                                {
                                                    switch (msg[counter])
                                                    {
                                                        case (byte)'\x1b':
                                                            {
                                                                counter++;

                                                                switch (msg[counter])
                                                                {
                                                                    case (byte)'\x54':
                                                                        {
                                                                            // Bold
                                                                            _DoPrintJobPublisher._PrintingFontStyle._FontStyle = FontStyle.Bold;
                                                                        }
                                                                        break;

                                                                    case (byte)'\x1f':
                                                                        {
                                                                            // Double Height considered as Double Size 
                                                                            _DoPrintJobPublisher._PrintingFontStyle._FontSize = 14;
                                                                            counter++; //temporary fix; TODO
                                                                        }
                                                                        break;

                                                                    case (byte)'\x61':
                                                                        {
                                                                            if (msg[counter++] == '\x01')
                                                                            {
                                                                                // Right Justify
                                                                                _DoPrintJobPublisher._PrintingFontStyle._HorizontalAlignment = HorizontalAlignment.Right;
                                                                            }
                                                                            else
                                                                            {
                                                                                // Center text
                                                                                _DoPrintJobPublisher._PrintingFontStyle._HorizontalAlignment = HorizontalAlignment.Center;
                                                                            }
                                                                        }
                                                                        break;

                                                                    case (byte)'\x0f':
                                                                        {
                                                                            // Half size
                                                                            _DoPrintJobPublisher._PrintingFontStyle._FontSize = 8;
                                                                        }
                                                                        break;
                                                                }
                                                            }
                                                            break;

                                                        case (byte)'\x12':
                                                            {
                                                                // Double size
                                                                _DoPrintJobPublisher._PrintingFontStyle._FontSize = 14;
                                                            }
                                                            break;

                                                        case (byte)'\x0e':
                                                            {
                                                                // Double Width considered as Double Size 
                                                                _DoPrintJobPublisher._PrintingFontStyle._FontSize = 14;
                                                            }
                                                            break;

                                                        default:
                                                            {
                                                                /* Do Nothing */
                                                            }
                                                            break;
                                                    }

                                                    counter++;
                                                }
                                                else
                                                {
                                                    rcvd[rcvdCounter] = msg[counter];
                                                    rcvdCounter++;
                                                    counter++;
                                                }
                                            }
                                        }

                                        List<int> PoundCharOcc = new List<int>();
                                        List<int> EuroCharOcc = new List<int>();
                                        List<int> ZlotyCharOcc = new List<int>();
                                        List<int> ZlotyUpperCharOcc = new List<int>();
                                        List<int> YenCharOcc = new List<int>();

                                        for (int Count = 0; Count < rcvdCounter; Count++)
                                        {
                                            if (rcvd[Count] == 0xA0 /* Special character for '£' */)
                                            {
                                                PoundCharOcc.Add(Count);
                                            }
                                            if (rcvd[Count] == 0x90 /* Special character for Zloty currency (l) */)
                                            {
                                                ZlotyCharOcc.Add(Count);
                                            }
                                            if (rcvd[Count] == 0x91 /* Special character for '¥' */)
                                            {
                                                YenCharOcc.Add(Count);
                                            }
                                            if (rcvd[Count] == 0x92 /* Special character for Zloty currency (L) */)
                                            {
                                                ZlotyUpperCharOcc.Add(Count);
                                            }
                                            if (rcvd[Count] == 0x93 /* Special character for '€' */)
                                            {
                                                EuroCharOcc.Add(Count);
                                            }
                                        }

                                        _DoPrintJobPublisher.LineToPrint = Encoding.GetEncoding(isostring).GetString(rcvd, 0, rcvdCounter);
                                        /* 
                                         * The character '£' is not supported in the ISO-8859-2. 
                                         * All occurence of '£' will be set manually as temporary fix.
                                         * also character '€' is not supported in the ISO-8859-2 so
                                         * All occurence of '€' will be set manually
                                         */
                                        if ((PoundCharOcc.Count > 0) || (EuroCharOcc.Count > 0) || (ZlotyCharOcc.Count > 0) || (YenCharOcc.Count > 0) || (ZlotyUpperCharOcc.Count > 0))
                                        {
                                            char[] TempBuffer = _DoPrintJobPublisher.LineToPrint.ToCharArray();
                                            if (PoundCharOcc.Count > 0)
                                            {
                                                for (int Count = 0; Count < PoundCharOcc.Count; Count++)
                                                {
                                                    TempBuffer[PoundCharOcc[Count]] = '£';
                                                }
                                            }

                                            if (ZlotyCharOcc.Count > 0)
                                            {
                                                for (int Count = 0; Count < ZlotyCharOcc.Count; Count++)
                                                {
                                                    TempBuffer[ZlotyCharOcc[Count]] = '\u0142';// Zloty Upper occuped 0xb3 position in iso-2
                                                }
                                            }

                                            if (YenCharOcc.Count > 0)
                                            {
                                                for (int Count = 0; Count < YenCharOcc.Count; Count++)
                                                {
                                                    TempBuffer[YenCharOcc[Count]] = '¥';
                                                }
                                            }

                                            if (ZlotyUpperCharOcc.Count > 0)
                                            {
                                                for (int Count = 0; Count < ZlotyUpperCharOcc.Count; Count++)
                                                {
                                                    TempBuffer[ZlotyUpperCharOcc[Count]] = '\u0141'; // Zloty Upper occuped 0xa3 position in iso-2
                                                }
                                            }

                                            if (EuroCharOcc.Count > 0)
                                            {
                                                for (int Count = 0; Count < EuroCharOcc.Count; Count++)
                                                {
                                                    TempBuffer[EuroCharOcc[Count]] = '€';
                                                }
                                            }

                                            _DoPrintJobPublisher.LineToPrint = new string(TempBuffer);
                                        }

                                        // Replace all non printable characters with space character
                                        _DoPrintJobPublisher.LineToPrint = ReplaceNonPrintableCharacters(_DoPrintJobPublisher.LineToPrint, ' ');

                                        _CriticalPrintJobSection = true;

                                        // Raise an event to display the request output
                                        //if (OnDoPrintJobOutput != null) OnDoPrintJobOutput(this, e);

                                        // Current command will be blocked waiting for updating PrintJob display terminated.
                                        while (_CriticalPrintJobSection == true)
                                        {
                                            System.Threading.Thread.Sleep(5);
                                        }

                                        // Increment the number of line
                                        _DoPrintJobNbLine++;
                                    }
                                }
                                //printInformation.PrintBufferIn = _DoPrintJobPublisher.LineToPrint;
                                //printInformation.Launch();
                                //Log.Info($"Printinformation status Out: {printInformation.StatusOut}");
                            }
                            if (messageOK)
                            {
                                //---- Send response
                                if (!ResponseSocketList.Contains(sock))
                                {
                                    ResponseSocketList.Add(sock);
                                }
                            }
                            else
                            {
                                //---- Close required 
                                ReadyToCloseList.Add(sock);

                                //---- Don't do any further processing
                                if (CheckWriteList.Contains(sock))
                                {
                                    CheckWriteList.Remove(sock);
                                }
                            }
                        }
                    }

                    //---- Process ready to write list
                    foreach (Socket sock in CheckWriteList)
                    {
                        // Generate response
                        // Generate response for this client
                        // (Little Endian Format)
                        byte[] rspBuf = new byte[17];
                        int Bufptr = 0;

                        // Add Header ;
                        rspBuf[Bufptr] = 9;
                        Bufptr++;
                        rspBuf[Bufptr] = 0;
                        Bufptr++;
                        rspBuf[Bufptr] = 0;
                        Bufptr++;
                        rspBuf[Bufptr] = 0;
                        Bufptr++;

                        // Add protocol version
                        rspBuf[Bufptr] = 0;
                        Bufptr++;
                        rspBuf[Bufptr] = 2;
                        Bufptr++;

                        // Add message type 
                        System.Array.Copy(BitConverter.GetBytes(DO_PRINTRESPONSE_TAG), 0, rspBuf, 6, 2);
                        Bufptr += 2;

                        Int32 LengthPrintJobStatus = 1;

                        // Add Tag
                        System.Array.Copy(BitConverter.GetBytes(TAG_ECR_PRINT_JOB_STATUS), 0, rspBuf, Bufptr, 4);
                        Bufptr += 4;

                        // Add Lentgh
                        System.Array.Copy(BitConverter.GetBytes(LengthPrintJobStatus), 0, rspBuf, Bufptr, 4);
                        Bufptr += 4;

                        // Add Value
                        rspBuf[Bufptr] = DoPrintJobStatus;
                        Bufptr++;
                        // Send response
                        sock.Send(rspBuf, 0, Bufptr, SocketFlags.None);
                        // Close
                        ReadyToCloseList.Add(sock);

                    }

                  


                    //---- Process ready to close list

                    foreach (Socket sock in ReadyToCloseList)
                    {
                        ClientSocketList.Remove(sock);

                        if (ResponseSocketList.Contains(sock))
                        {
                            ResponseSocketList.Remove(sock);
                        }
                        sock.Close();
                    }
                }

                
            }
            catch (Exception ex)
            {
                // Address binding exception
                Log.Error(ex.StackTrace);
            }
        }

        private string ReplaceNonPrintableCharacters(String msg, char replaceWith)
        {
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < msg.Length; i++)
            {
                char c = msg[i];
                int b = (int)c;

                if (b < 32)
                {
                    result.Append(replaceWith);
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        public void StopPrintServer()
        {
            foreach (Socket sock in ClientSocketList)
            {
                sock.Close(); // Close any client sockets
            }

            if (ServerSocket != null)
            {
                ServerSocket.Close(); // Close the server socket
            }
        }

        private static int ParseHeader(byte[] Data)
        {
            /* Local variable */
            byte[] message_length;
            byte[] protocol_version;

            int count = 0;
            message_length = new byte[PROTOCOL_MESSAGE_SIZE];
            protocol_version = new byte[PROTOCOL_VERSION_SIZE];

            /* Program */
            // Get the message data length
            System.Array.Copy(Data, message_length, PROTOCOL_MESSAGE_SIZE);
            count += PROTOCOL_MESSAGE_SIZE;

            // Get the protocol version
            System.Array.Copy(Data, count, protocol_version, 0, PROTOCOL_VERSION_SIZE);

            // Test the compatibility of received protocol version
            if (BitConverter.ToInt16(protocol_version, 0) > PROTOCOL_VERSION)
            {
                // Return error in protocol version
                return -1;
            }

            // Return the length of message to be received after
            return BitConverter.ToInt32(message_length, 0);
        }

        public static byte[] TLVFieldUnpack(byte[] sourceBuf, int avail, ref byte[] Tag, ref byte[] Length)
        {
            Int16 type;
            int count;
            int MsgLen;
            int MsgSize;
            byte[] Bufresult;

            Bufresult = new byte[avail];

            if (avail >= PROTOCOL_HEADER_SIZE)
            {
                // Get header length
                MsgLen = ParseHeader(sourceBuf);

                if (avail == (PROTOCOL_BEGIN_DATA_TLV + MsgLen))
                {
                    type = BitConverter.ToInt16(sourceBuf, PROTOCOL_HEADER_SIZE);

                    if (type == DO_PRINTREQUEST_TAG)
                    {
                        // print request message type
                        if (MsgLen < (PROTOCOL_TLV_TAG_SIZE + PROTOCOL_TLV_LENGTH_SIZE))
                        {
                            // bad request message format
                        }
                        else
                        {
                            count = PROTOCOL_BEGIN_DATA_TLV;

                            MsgSize = MsgLen;

                            if (MsgSize >= (PROTOCOL_TLV_TAG_SIZE + PROTOCOL_TLV_LENGTH_SIZE))
                            {
                                // Get tag
                                System.Array.Copy(sourceBuf, count, Tag, 0, PROTOCOL_TLV_TAG_SIZE);
                                count += PROTOCOL_TLV_TAG_SIZE;

                                // Get length
                                System.Array.Copy(sourceBuf, count, Length, 0, PROTOCOL_TLV_LENGTH_SIZE);
                                count += PROTOCOL_TLV_LENGTH_SIZE;

                                // Verify data or value existence
                                MsgSize -= (PROTOCOL_TLV_TAG_SIZE + PROTOCOL_TLV_LENGTH_SIZE);

                                if (String.Compare(Length.ToString(), "0") != 0)
                                {
                                    // Copy raw data to be printed
                                    System.Array.Copy(sourceBuf, count, Bufresult, 0, avail - count);
                                }
                            }
                        }
                    }
                }
            }

            return Bufresult;
        }
    }


    public class DoPrintJobPublisher
    {
        public string LineToPrint { get; set; }
        public PrintingFontStyle _PrintingFontStyle;

        public DoPrintJobPublisher()
        {
            _PrintingFontStyle = null;
            LineToPrint = "";
        }
    }

    public class PrintingFontStyle
    {
        public string _FontName { get; set; }
        public int _FontSize { get; set; }
        public Color _FontColor { get; set; }
        public FontStyle _FontStyle { get; set; }
        public HorizontalAlignment _HorizontalAlignment { get; set; }

        public PrintingFontStyle()
        {
            _FontName = "Courier New";
            _FontSize = 10;
            _FontColor = Color.Black;
            _FontStyle = System.Drawing.FontStyle.Regular;
            _HorizontalAlignment = HorizontalAlignment.Left;
        }
    }

}
