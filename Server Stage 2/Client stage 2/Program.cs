using System;
using System.Net.Sockets;
using System.Text;
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


            Console.WriteLine("Введите запрос (GET, PUT, DELETE)");
            string action = Console.ReadLine().ToUpper();


            if (action == "PUT")
            {

                Console.WriteLine("Введите файл с вашей папки:");
                string name = Console.ReadLine();
                string path = Path.Combine(@"C:\Users\rusla\client\data", name);


                if (File.Exists(path))
                {

                    byte[] fileData = await File.ReadAllBytesAsync(path);
                    byte[] lengthBytes = BitConverter.GetBytes(fileData.Length);

                    Console.WriteLine("Введите название файла под которым будет сохранен файл на сервере:");
                    string NewName = Console.ReadLine();


                    if (NewName == "" || NewName == "\n")
                    {
                        string[] exm = name.Split('.');
                        string path1 = @"C:\Users\rusla\client\data";
                        NewName = GenerateUniqueFileName(path1, $"example.{exm[1]}");
                    }


                    byte[] data = System.Text.Encoding.UTF8.GetBytes($"{action},{NewName}");
                    await socket.SendAsync(data, SocketFlags.None);


                    using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        long fileSize = fileStream.Length;
                        byte[] sizeBuffer = BitConverter.GetBytes(fileSize);
                        await socket.SendAsync(sizeBuffer, SocketFlags.None);

                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await socket.SendAsync(buffer, SocketFlags.None);
                        }
                    }
                }


                else { Console.WriteLine("Такого файла нет на клиентском компьютере"); return; }
            }



            else if (action == "GET")
            {
                Console.WriteLine("Хотите получить файл по имени или ID?(ID, NAME):");
                string By = Console.ReadLine().ToUpper();
                if (By == "ID")
                {
                    Console.WriteLine("Введите ID");
                    string id = Console.ReadLine();
                    byte[] data = System.Text.Encoding.UTF8.GetBytes($"{action},{By},{id}");
                    await socket.SendAsync(data, SocketFlags.None);
                }
                else if (By == "NAME")
                {
                    Console.WriteLine("Введите название файла с его расширением");
                    string name = Console.ReadLine();
                    byte[] data = System.Text.Encoding.UTF8.GetBytes($"{action},{By},{name}");
                    await socket.SendAsync(data, SocketFlags.None);
                }
                Console.WriteLine("Запрос был отправлен на сервер!");
            }

            else if (action == "DELETE")
            {
                Console.WriteLine("Хотите удалить файл по имени или ID?(ID, NAME):");
                string By = Console.ReadLine().ToUpper();
                if (By == "ID")
                {
                    Console.WriteLine("Введите ID");
                    string id = Console.ReadLine();
                    byte[] data = System.Text.Encoding.UTF8.GetBytes($"{action},{By},{id}");
                    await socket.SendAsync(data, SocketFlags.None);
                }
                else if (By == "NAME")
                {
                    Console.WriteLine("Введите название файла с его расширением");
                    string name = Console.ReadLine();
                    byte[] data = System.Text.Encoding.UTF8.GetBytes($"{action},{By},{name}");
                    await socket.SendAsync(data, SocketFlags.None);
                }
                Console.WriteLine("Запрос был отправлен на сервер!");
            }

            else if (action == "EXIT")
            {
                await socket.SendAsync(Encoding.UTF8.GetBytes($"{action}"), SocketFlags.None);
            }




            if (action == "PUT")
            {
                byte[] buffer = new byte[1024];
                int bytesReceived = await socket.ReceiveAsync(buffer, SocketFlags.None);
                string[] result = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesReceived).Split(',');
                if (result[0] == "200")
                {
                    Console.WriteLine("The response says that file was created!\t" + result[1]);
                }
                else if (result[0] == "403")
                {
                    Console.WriteLine("The response says that creating the file was forbidden!");
                }
            }





            else if (action == "GET")
            {
                // Получаем строку от сервера
                byte[] responseBytes = new byte[3]; // "200" занимает 3 байта
                await socket.ReceiveAsync(responseBytes, SocketFlags.None);
                string response = System.Text.Encoding.UTF8.GetString(responseBytes);

                if (response == "200")
                {

                    Console.WriteLine("Введите название под которым сохранится файл:");
                    string name = Console.ReadLine();
                    byte[] lengthBytes = new byte[sizeof(int)];
                    await socket.ReceiveAsync(lengthBytes, SocketFlags.None);
                    int byteArrayLength = BitConverter.ToInt32(lengthBytes);

                    // Получаем массив байтов от сервера
                    byte[] buffer1 = new byte[byteArrayLength];
                    await socket.ReceiveAsync(buffer1, SocketFlags.None);

                    string path = $@"C:\Users\rusla\client\data\{name}";
                    File.WriteAllBytes(path, buffer1);
                    Console.WriteLine("Файл скачан к вам на компьютер");
                }


                else if (response == "404")
                {
                    Console.WriteLine("The response says that the file was not found!");
                }
            }




            else if (action == "DELETE")
            {
                byte[] buffer = new byte[1024];
                int bytesReceived = await socket.ReceiveAsync(buffer, SocketFlags.None);
                string result = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                if (result == "200")
                {
                    Console.WriteLine("The response says that file was deleted!");
                }
                else if (result == "404")
                {
                    Console.WriteLine("The response says that file not found!");
                }
            }

        }
        catch (SocketException)
        {
            Console.WriteLine($"Не удалось установить подключение с {socket.RemoteEndPoint}");
        }
    }
    static string GenerateUniqueFileName(string directoryPath, string baseFileName)
    {
        string uniqueFileName = baseFileName;
        int counter = 1;

        // Получаем полный путь к файлу
        string filePath = Path.Combine(directoryPath, uniqueFileName);

        // Проверяем, существует ли файл с таким именем
        while (File.Exists(filePath))
        {
            // Если файл существует, генерируем новое уникальное имя
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFileName);
            string fileExtension = Path.GetExtension(baseFileName);
            uniqueFileName = $"{fileNameWithoutExtension}_{counter}{fileExtension}";

            // Обновляем полный путь к файлу
            filePath = Path.Combine(directoryPath, uniqueFileName);

            counter++;
        }

        return uniqueFileName;
    }
}
