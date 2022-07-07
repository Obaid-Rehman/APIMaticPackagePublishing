using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO.Compression;

namespace PythonPackagePublishing.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublishController : ControllerBase
    {
        private readonly ILogger<PublishController> _logger;
        private readonly IConfiguration _configuration;
        public PublishController(ILogger<PublishController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Publish([FromForm] IFormFile data)
        {
            if (data == null)
            {
                string message = "File can not be null";
                _logger.LogError(message);
                return BadRequest(message);
            }
            var size = data.Length;
            if (size == 0)
            {
                string message = "File can not be empty";
                _logger.LogError(message);
                return BadRequest(message);
            }

            var sourceFilePath = Path.GetTempFileName();
            using (var stream = System.IO.File.Create(sourceFilePath))
            {
                await data.CopyToAsync(stream);
            }

            var targetDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            var zipFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            var stdStandardOutput = "PIP_INSTALL Info:\n";
            var stdErrorOutput = "PIP_INSTALL Errors:\n";
            var stdErrorOutputInitialLength = stdErrorOutput.Length;

            try
            {
                ZipFile.ExtractToDirectory(sourceFilePath, targetDir);

                string pipInstallationDirectory = _configuration["PIP_INSTALLATION_DIR"];
                string pipExecutable = OperatingSystem.IsLinux() ? "pip" : "pip.exe";

                var installationDir = Path.Combine(targetDir, "pipInstallations");
                Directory.CreateDirectory(installationDir);

                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    FileName = Path.Combine(pipInstallationDirectory, pipExecutable),
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    Arguments = $"install -r requirements.txt -t {installationDir}",
                    WorkingDirectory = targetDir,
                };

                using (Process exeProcess = Process.Start(startInfo))
                {
                    stdStandardOutput += await exeProcess.StandardOutput.ReadToEndAsync();
                    stdErrorOutput += await exeProcess.StandardError.ReadToEndAsync();
                    exeProcess.WaitForExit();
                }

                _logger.LogInformation(stdStandardOutput);
                if (stdErrorOutput.Length > stdErrorOutputInitialLength)
                {
                    _logger.LogError(stdErrorOutput);
                }

                ZipFile.CreateFromDirectory(targetDir, zipFilePath, CompressionLevel.Optimal, false, new ZipEncoder());

                using (var stream = System.IO.File.OpenRead(zipFilePath))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        var numBytes = new FileInfo(zipFilePath).Length;
                        var buff = reader.ReadBytes((int)numBytes);
                        return File(buff, "application/zip", "response.zip");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Something went wrong. Exeception:\n{e.StackTrace}");
                return BadRequest(e.StackTrace);
            }
            finally
            {
                Directory.Delete(targetDir, true);
                System.IO.File.Delete(sourceFilePath);
                if (System.IO.File.Exists(zipFilePath))
                {
                    System.IO.File.Delete(zipFilePath);
                }
            }
        }
    }
}
