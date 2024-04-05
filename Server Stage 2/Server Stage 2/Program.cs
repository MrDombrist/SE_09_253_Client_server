using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

class Program
{   public static bool flag = true;
    static async Task Main(string[] args)
    {
        string directoryPath = @"C:\Users\rusla\server\data";
        string bumps = @"C:\Users\rusla\bumps\bumps.txt";

        IPEndPoint ipPoint = new IPEndPoint(IPAddress.Any, 8888);

        Dictionary<string, string> bumpsfile = new Dictionary<string, string>();
        string[] lines = File.ReadAllLines(bumps);
        foreach (string line in lines)
        {
            string[] parts = line.Split(',');
            if (parts.Length == 2)
            {
                bumpsfile.Add(parts[1], parts[0]);
            }
        }


        using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        Console.WriteLine(ipPoint);
        socket.Bind(ipPoint);
        socket.Listen();
        Console.WriteLine("Сервер запущен. Ожидание подключений...");

        while (flag)
        {
            Socket client = await socket.AcceptAsync();
            _ = Task.Run(() => ProcessClientAsync(client, directoryPath, bumpsfile, socket));
        } 
        
       
    }   

    static async Task ProcessClientAsync(Socket client, string directoryPath, Dictionary<string,string> bumps,Socket socket)
    {
        try
        {
            Console.WriteLine($"Адрес подключенного клиента: {client.RemoteEndPoint}");

            byte[] buffer = new byte[1024];
            int bytesReceived = await client.ReceiveAsync(buffer, SocketFlags.None);
            string data = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
            
            Console.WriteLine($"Клиент передал запрос: {data}");

            string[] request = data.Split(',');

            if (request[0] == "PUT")
            {
                await HandlePutRequestAsync(client, request, directoryPath, bumps);
            }
            else if (request[0] == "GET")
            {
                await HandleGetRequestAsync(client, request, directoryPath,bumps);
            }
            else if (request[0] == "DELETE")
            {
                await HandleDeleteRequestAsync(client, request, directoryPath, bumps);
            }
            else if(data == "EXIT" || request[0] == "EXIT")
            {
                flag=false;
                socket.Close();
                StringBuilder sb = new StringBuilder();
                foreach (var pair in bumps)
                {
                    sb.AppendLine($"{pair.Key},{pair.Value}");
                }
                File.WriteAllText(@"C:\Users\rusla\bumps\bumps.txt", sb.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки клиента: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    static async Task HandlePutRequestAsync(Socket client, string[] request, string directoryPath, Dictionary<string,string> bumps)
    {
        try
        {
            string fileName = request[1];
            string filePath = Path.Combine(directoryPath, fileName);

            byte[] lenghtBytes = new byte[sizeof(long)]; // Изменено на sizeof(long) для передачи размера файла
            await client.ReceiveAsync(lenghtBytes, SocketFlags.None);
            long dataSize = BitConverter.ToInt64(lenghtBytes); // Изменено на long для передачи размера файла

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                    long bytesReceived = 0;
                    int chunkSize = 1024; // Размер части данных, которую будем принимать за один раз
                    byte[] buffer = new byte[chunkSize];
                    while (bytesReceived < dataSize)
                    {
                        int bytesRead = await client.ReceiveAsync(buffer, SocketFlags.None);
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        bytesReceived += bytesRead;
                    }
            }

            if (File.Exists(filePath))
            {
                
                Guid guid = Guid.NewGuid();
                bumps.Add(guid.ToString(),fileName);

                byte[] otklik = System.Text.Encoding.UTF8.GetBytes($"200,{guid}");
                await client.SendAsync(otklik, SocketFlags.None);
            }
            else
            {
                await client.SendAsync(Encoding.UTF8.GetBytes("403"), SocketFlags.None);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки PUT запроса: {ex.Message}");
        }
    }

    static async Task HandleGetRequestAsync(Socket client, string[] request, string directoryPath,Dictionary<string,string> bumps)
    {
        try
        { string filem = request[2];
            if (request[1] == "ID") { filem = FindById(request[2]); }
            string filePath = Path.Combine(directoryPath, filem);
            if (File.Exists(filePath))
            {
                byte[] bytes = await File.ReadAllBytesAsync(filePath);
                byte[] lengthBytes = BitConverter.GetBytes(bytes.Length);

                await client.SendAsync(Encoding.UTF8.GetBytes("200"), SocketFlags.None);
                await client.SendAsync(lengthBytes, SocketFlags.None);
                await client.SendAsync(bytes, SocketFlags.None);
            }
            else
            {
                await client.SendAsync(Encoding.UTF8.GetBytes("404"), SocketFlags.None);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки GET запроса: {ex.Message}");
        }
    }

    static async Task HandleDeleteRequestAsync(Socket client, string[] request, string directoryPath, Dictionary<string,string> bumps)
    {
        try
        {   
            string filem = request[2];
            if (request[1] == "ID")
            {
                var item = bumps.FirstOrDefault(kvp => kvp.Value == filem);
                filem = item.Key;
            }
                string path = Path.Combine(directoryPath,filem);
            if (File.Exists(path))
            {
                File.Delete(path);
                bumps.Remove(filem);
                await client.SendAsync(Encoding.UTF8.GetBytes("200"), SocketFlags.None);
            }
            else
            {
                await client.SendAsync(Encoding.UTF8.GetBytes("404"), SocketFlags.None);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки DELETE запроса: {ex.Message}");
        }
    }

    static string FindById(string id)
    {
        string[] content = File.ReadAllLines(@"C:\Users\rusla\bumps\bumps.txt");
        foreach (string line in content)
        {
            if (!string.IsNullOrEmpty(line))
            {
                string[] parts = line.Split(',');
                if (parts[1] == id)
                {
                    return parts[0];
                }
            }
        }
        return "error";
    }
}
