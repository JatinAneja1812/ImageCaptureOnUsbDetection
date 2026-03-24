
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

class Program
{
    static void Main()
    {
        string baseFolder = AppContext.BaseDirectory;
        string psPath = Path.Combine(baseFolder, "UsbDetect.ps1");

        if(psPath == null || !File.Exists(psPath))
        {
            Console.WriteLine($"PowerShell script not found at {psPath}");
            return;
        }

        Console.WriteLine("Waiting for USB (PowerShell)...");
        RunPowerShellAndWait(psPath);   // blocks until USB is detected

        int camIndex = FindFirstWorkingCamera();
        if (camIndex == -1)
        {
            Console.WriteLine("No camera available, aborting snapshot.");
            return;
        }

        Console.WriteLine("USB detected. Taking snapshot...");

        TakeSnapshot(baseFolder, camIndex);

        Console.WriteLine("Done. Press any key to exit.");
        Console.ReadKey();
    }

    static void RunPowerShellAndWait(string scriptPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-ExecutionPolicy Bypass -NoProfile -File \"{scriptPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi)!;
        proc.OutputDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.WaitForExit();
    }

    static void TakeSnapshot(string baseFolder, int cameraIndex)
    {
        using var capture = new VideoCapture(cameraIndex, VideoCaptureAPIs.DSHOW);
        if (!capture.IsOpened())
        {
            Console.WriteLine($"Camera {cameraIndex} not available.");
            return;
        }

        Thread.Sleep(1000);

        using var frame = new Mat();
        capture.Read(frame);
        if (frame.Empty())
        {
            Console.WriteLine("Failed to read frame from camera.");
            return;
        }

        using Bitmap bmp = BitmapConverter.ToBitmap(frame);
        string path = Path.Combine(baseFolder,
            $"snapshot_{cameraIndex}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        bmp.Save(path, ImageFormat.Png);
        Console.WriteLine($"Snapshot saved to {path}");
    }

    static int FindFirstWorkingCamera(int maxIndex = 10)
    {
        for (int i = 0; i < maxIndex; i++)
        {
            using var cap = new OpenCvSharp.VideoCapture(i, OpenCvSharp.VideoCaptureAPIs.DSHOW);
            if (!cap.IsOpened())
            {
                Console.WriteLine($"Camera index {i}: not opened");
                continue;
            }

            using var frame = new OpenCvSharp.Mat();
            cap.Read(frame);
            if (frame.Empty())
            {
                Console.WriteLine($"Camera index {i}: opened but no frames");
                continue;
            }

            Console.WriteLine($"Camera index {i}: OK (resolution {frame.Width}x{frame.Height})");
            return i;
        }

        Console.WriteLine("No working camera found.");
        return -1;
    }

}
