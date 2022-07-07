using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO.Compression;

namespace RubyPackagePublishing.Controllers
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

            var stdStandardOutput = "GEM_BUILD Info:\n";
            var stdErrorOutput = "GEM_BUILD Errors:\n";
            var stdErrorOutputInitialLength = stdErrorOutput.Length;

            try
            {
                ZipFile.ExtractToDirectory(sourceFilePath, targetDir);

                string gemInstallationDirectory = _configuration["GEM_INSTALLATION_DIR"];
                string gemExecutable = OperatingSystem.IsLinux() ? "gem" : "gem.cmd";

                string gemspecFilePath = Directory.EnumerateFiles(targetDir).First(x => x.EndsWith(".gemspec"));

                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    FileName = Path.Combine(gemInstallationDirectory, gemExecutable),
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    Arguments = $"build -V {gemspecFilePath}",
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

                string gemFilePath = Directory.EnumerateFiles(targetDir).First(x => x.EndsWith(".gem"));
                startInfo.Arguments = $"push -V {gemFilePath}";
                startInfo.Environment.Add("GEM_HOST_API_KEY", "rubygems_fdbdc7f25e00839b233311268327a4369c95e4a1fa48d8e1");

                stdStandardOutput = "GEM_PUSH Info:\n";
                stdErrorOutput = "GEM_PUSH Errors:\n";
                stdErrorOutputInitialLength = stdErrorOutput.Length;

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
