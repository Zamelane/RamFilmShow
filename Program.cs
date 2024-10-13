using ByteSizeLib;
using OneOf.Types;
using RamDrive.OsfMount;
using System.Diagnostics;
using System.Text;

namespace RamFilmShow
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var DL = DriveLetter.X;
            double size = 1.5;

            string filesPath = @$"{DL}:\";

            Console.WriteLine($"Выбран автоматический размер диска: {size}GB");

            await OsfMountRamDrive.ForceUnmount(DL); // Размонтируем, если уже монтировали

            Console.Write("Введите ссылку на видео, которое качаем: ");
            var url = Console.ReadLine();

            Console.WriteLine("Монтирую виртуальный диск ...");

            var possibleMountError = await OsfMountRamDrive.Mount(
                ByteSize.FromGibiBytes(size),
                DL,
                FileSystemType.NTFS
            );

            Console.WriteLine("Пробуем качать ...");

            string arg = $"-o \"%(title)s.%(ext)s\" -P \"X:\\\\\" \"{url}\"";

            bool isDownloadProcLog = false;

            var process = new Process();
            process.StartInfo.FileName = "yt-dlp.exe";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.Arguments = arg;

            process.OutputDataReceived += (s, a) =>
            {
                if (!String.IsNullOrEmpty(a.Data))
                {
                    string data = a.Data;

                    if (data.IndexOf("[download]") > -1 && data.IndexOf("at") > 0 && data.IndexOf("%") > 0)
                    {
                        if (!isDownloadProcLog)
                            isDownloadProcLog = true;
                        else Console.SetCursorPosition(0, Console.CursorTop - 1);
                        Console.WriteLine(data);
                    }
                }
            };
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();

            Console.WriteLine("Пробую открыть конечный каталог загрузки ...");

            if (!Path.Exists(filesPath))
                Console.WriteLine("Почему-то конечный каталог не найден ...");
            else
            {
                var explorerProc = Process.Start("explorer.exe", filesPath);
                explorerProc.WaitForExit();
            }

            Console.WriteLine("Нажмите любую клавишу, чтобы размонтировать диск ...");
            Console.ReadKey();
            Console.WriteLine("Размонтирую диск ...");

            var possibleUnmountError = await OsfMountRamDrive.ForceUnmount(DL);
        }
    }
}
