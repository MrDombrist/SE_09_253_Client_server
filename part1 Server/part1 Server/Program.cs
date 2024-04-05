using System;
using System.Net;
using System.Net.Sockets;

class Program
{
    static void Main(string[] args)
    {
        bool flag = true;
        IPEndPoint ipPoint = new IPEndPoint(IPAddress.Any, 8888);

        using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        Console.WriteLine(ipPoint);
        socket.Bind(ipPoint);
        socket.Listen();
        Console.WriteLine("Сервер запущен. Ожидание подключений...");
        while (flag)
        {
            using Socket client = socket.Accept();
            Console.WriteLine($"Адрес подключенного клиента: {client.RemoteEndPoint}");

            byte[] buffer = new byte[128];
            int bytesReceived = client.Receive(buffer);
            string data = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesReceived);
            if (data == "exit")
            {
                socket.Close();
                flag = false;
            }
            Console.WriteLine($"Клиент передал запрос: {data}");
            string[] request = data.Split(',');
            if (request[0] == "PUT")
            {
                string path = $@"C:\Users\rusla\server\data\{request[1]}";
                
                if (File.Exists(path))
                {
                    client.Send(System.Text.Encoding.UTF8.GetBytes("403"));
                    Console.WriteLine("403");
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(path))
                    {
                        //File.Create(path);
                        sw.WriteLine(request[2]);
                        client.Send(System.Text.Encoding.UTF8.GetBytes("200"));
                        Console.WriteLine("200");
                    }
                }
            }
            else if(request[0] == "DELETE")
            {
                string path = $@"C:\Users\rusla\server\data\{request[1]}";
                if (File.Exists(path))
                {
                    File.Delete(path);
                    client.Send(System.Text.Encoding.UTF8.GetBytes("200"));
                    Console.WriteLine("200");
                }
                else { client.Send(System.Text.Encoding.UTF8.GetBytes("404")); Console.WriteLine("404"); ; }
            }
            else if (request[0] == "GET")
            {
                string path = $@"C:\Users\rusla\server\data\{request[1]}";
                if (File.Exists(path))
                {
                    using(StreamReader sr = new StreamReader(path))
                    {
                        string a =sr.ReadToEnd();
                        client.Send(System.Text.Encoding.UTF8.GetBytes("200,"+a));
                        Console.WriteLine("200");
                    } 
                }
                else
                {
                    client.Send(System.Text.Encoding.UTF8.GetBytes("404"));
                }
            }
            
            
        }
    }
}