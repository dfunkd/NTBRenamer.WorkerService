using System.Diagnostics;

public class BatchFileExecutor
{
    public static void RunGhostscriptBatchFile(string batchFilePath, string directory, string arguments = "")
    {
        if (!File.Exists(batchFilePath))
            throw new FileNotFoundException($"Batch file not found: {batchFilePath}");

        try
        {
            // Create a new ProcessStartInfo object
            ProcessStartInfo startInfo = new()
            {
                Arguments = $"/C \"{batchFilePath}\" {arguments}", // /C executes the command and then terminates
                CreateNoWindow = true, // Do not create a new window for the process
                FileName = "cmd.exe", // Specify the command shell
                RedirectStandardError = true,
                RedirectStandardOutput = true, // Redirect output to capture it
                UseShellExecute = false, // Do not use the shell to execute
                WorkingDirectory = directory
            };

            // Start the process
            using Process? process = Process.Start(startInfo);

            if (process is not null)
            {
                // Optionally, read the output
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                int exitCode = process.ExitCode;

                process.WaitForExit(); // Wait for the process to complete

                /*
                if (process.ExitCode != 0)
                {
                    // Handle errors if the batch file failed
                    Console.WriteLine($"Batch file exited with code: {process.ExitCode}");
                    Console.WriteLine($"Output: {output}");
                }
                else
                    Console.WriteLine($"Batch file executed successfully. Output: {output}");
                */
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing batch file: {ex.Message}");
        }
    }

    public static void ConvertToPdf(string inputFilePath, string outputPdfPath)
    {
        string dllPath = @"C:\Users\dfunk\OneDrive - Ruan Transportation Management Systems\Desktop\gs\gs10.05.1\bin\gswin64c.exe";
        // Ensure the input file exists
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");

        // Ensure the Ghostscript DLL path is valid
        if (!File.Exists(dllPath))
            throw new FileNotFoundException($"Ghostscript DLL not found: {dllPath}");

        // Construct Ghostscript arguments for PDF conversion
        // -q: Quiet mode (suppress startup messages)
        // -dNOPAUSE -dBATCH: Prevent Ghostscript from pausing and exit after processing
        // -sDEVICE=pdfwrite: Specify the PDF output device
        // -sOutputFile=<output_path>: Specify the output PDF file path
        // <input_path>: Specify the input file
        string args = $"-q -dNOPAUSE -dBATCH -sDEVICE=pdfwrite -sOutputFile=\"{outputPdfPath}\" \"{inputFilePath}\"";

        try
        {
            // Create a process to run Ghostscript
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = dllPath, // Path to gsdll32.dll or gsdll64.dll
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using Process process = Process.Start(startInfo);
            process.WaitForExit();

            // Check for errors
            if (process.ExitCode != 0)
                throw new Exception($"Ghostscript conversion failed with exit code {process.ExitCode}. Error: {process.StandardError.ReadToEnd()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during PDF conversion: {ex.Message}");
            throw; // Re-throw the exception for further handling
        }
    }
}