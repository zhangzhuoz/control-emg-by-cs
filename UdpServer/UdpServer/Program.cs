using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Diagnostics;
using System.IO;

using AnalogIO;
using MccDaq;
using ErrorDefs;


public struct Received
{
    public IPEndPoint Sender;
    public string Message;
}

abstract class UdpBase
{
    protected UdpClient Client;

    protected UdpBase()
    {
        Client = new UdpClient();
    }

    public async Task<Received> Receive()
    {
        var result = await Client.ReceiveAsync();
        return new Received()
        {
            Message = Encoding.ASCII.GetString(result.Buffer, 0, result.Buffer.Length),
            Sender = result.RemoteEndPoint
        };
    }
}

//Server
class UdpListener : UdpBase
{
    private IPEndPoint _listenOn;

    public UdpListener() : this(new IPEndPoint(IPAddress.Any, 8001))
    {
    }

    public UdpListener(IPEndPoint endpoint)
    {
        _listenOn = endpoint;
        Client = new UdpClient(_listenOn);
    }

    public void Reply(string message, IPEndPoint endpoint)
    {
        var datagram = Encoding.ASCII.GetBytes(message);
        Client.Send(datagram, datagram.Length, endpoint);
    }
}

//Client
class UdpUser : UdpBase
{
    private UdpUser() { }

    public static UdpUser ConnectTo(string hostname, int port)
    {
        var connection = new UdpUser();
        connection.Client.Connect(hostname, port);
        return connection;
    }

    public void Send(string message)
    {
        var datagram = Encoding.ASCII.GetBytes(message);
        Client.Send(datagram, datagram.Length);
    }
}

//class Program
//{
//    static void Main(string[] args)
//    {
//        //create a new server
//        var server = new UdpListener();

//        //start listening for messages and copy the messages back to the client
//        Task.Factory.StartNew(async () => {
//            while (true)
//            {
//                var received = await server.Receive();
//                server.Reply("copy " + received.Message, received.Sender);
//                if (received.Message == "quit")
//                    break;
//            }
//        });



//        //type ahead :-)
//        string read;
//        do
//        {
//            read = Console.ReadLine();
//            client.Send(read);
//        } while (read != "quit");
//    }
//}


namespace UDP
{
    class Program
    {

        static MccDaq.MccBoard DaqBoard = new MccDaq.MccBoard(0);

        private int str_temp;
        private static MccDaq.Range Range;        //定义A/D和D/A转换范围

        const int NumPoints = 5000;     //  Number of data points to collect
        const int ArraySize = 5000;       //  size of data array
        private ushort[] DataBuffer;    //  declare data array
        string FileName;                //  name of file in which data will be stored

        AnalogIO.clsAnalogIO AIOProps = new AnalogIO.clsAnalogIO();


        int Count = NumPoints;
        //  it may be necessary to add path to file name for data file to be found
        int Rate = 1000;
        int LowChan = 0;
        int HighChan = 0;
        MccDaq.ScanOptions Options = MccDaq.ScanOptions.Default;

        private int NumAIChans;
        private int ADResolution;




        static void Main(string[] args)
        {

            //得到本机IP，设置TCP端口号         

            var server = new UdpListener();


            //绑定网络地址

            Console.WriteLine("This is a Server, host name is {0}", Dns.GetHostName());

            //等待客户机连接
            Console.WriteLine("Waiting for a client");
            

            

            float EngUnits;
            double HighResEngUnits;
            MccDaq.ErrorInfo ULStat;//存储和报告错误代码和消息
            System.UInt16 DataValue;
            System.UInt32 DataValue32;
            int Chan = 0;     //输入通道编号
            int Options = 0;

            //start listening for messages and copy the messages back to the client
            Task.Factory.StartNew(async () => {
                while (true)
                {
                    var received = await server.Receive();
                    
                    Console.WriteLine("Got this:" + received.Message);

                    ULStat = DaqBoard.AIn(Chan, Range, out DataValue);//读取输入通道，输出16位整数值
                                                                      //  Convert raw data to Volts by calling ToEngUnits
                                                                      //  (member function of MccBoard class)
                    ULStat = DaqBoard.ToEngUnits(Range, DataValue, out EngUnits);//将原始数据转换成电压

                    int barHeight = (int)Math.Ceiling(EngUnits * 1000 + 150);

                    server.Reply(barHeight.ToString(), received.Sender);
                    if (received.Message == "quit")
                        break;
                }
            });



            ////create a new client
            //var client = UdpUser.ConnectTo("127.0.0.1", 8001);

            ////wait for reply messages from server and send them to console
            //Task.Factory.StartNew(async () =>
            //{
            //    while (true)
            //    {
            //        try
            //        {
            //            var received = await client.Receive();
            //            Console.WriteLine(received.Message);
            //            if (received.Message.Contains("quit"))
            //                break;
            //        }
            //        catch (Exception ex)
            //        {
            //            Debug.Write(ex);
            //        }
            //    }
            //});


            //type ahead :-)
            string read;
            do
            {
                read = Console.ReadLine();
            } while (read != "quit");
            
        }

    }
}