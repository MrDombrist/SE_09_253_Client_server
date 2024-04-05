using System;
using System.Net.Sockets;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync("127.0.0.1", 8888);
            Console.WriteLine($"Подключение к {socket.RemoteEndPoint} установлено");

            Console.WriteLine("Введите запрос (GET, PUT, DELETE), название файла и содержимое через запятую");
            string input = Console.ReadLine();
            byte[] data = System.Text.Encoding.UTF8.GetBytes(input);
            await socket.SendAsync(data, SocketFlags.None);

            byte[] buffer = new byte[1024];
            int bytesReceived = await socket.ReceiveAsync(buffer, SocketFlags.None);
            string result = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesReceived);
            if (input[0] == 'P')
            {
                if (result == "200")
                {
                    Console.WriteLine("The response says that file was created!");
                }
                else if (result == "403")
                {
                    Console.WriteLine("The response says that creating the file was forbidden!");
                }
            }
            else if (input[0] == 'D')
            {
                if (result == "200")
                {
                    Console.WriteLine("The response says that the file was successfully deleted!");
                }
                else if (result == "404")
                {
                    Console.WriteLine(" The response says that the file was not found!");
                }
            }
            else if (input[0] == 'G')
            {
                string[] res = result.Split(',');
                if (res[0] == "200")
                {
                    Console.WriteLine($"The content of the file is:{res[1]}");
                }
                else if (res[0] == "404")
                {
                    Console.WriteLine("The response says that the file was not found!");
                }
            }

        }
        catch (SocketException)
        {
            Console.WriteLine($"Не удалось установить подключение с {socket.RemoteEndPoint}");
        }
    }
}
