using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace AzureWorkshop.Controllers
{
    public class HomeController : Controller
    {
	    private readonly IHostingEnvironment hostingEnvironment;
	    private readonly ILogger<HomeController> logger;
	    public HomeController(IHostingEnvironment hostingEnvironment,
		    ILogger<HomeController> logger)
	    {
		    this.hostingEnvironment = hostingEnvironment;
		    this.logger = logger;
	    }

		public IActionResult Index()
        {
            return View();
        }

	    [HttpPost]
	    public IActionResult Upload(IFormFile formFile)
	    {
		    var storeFile = CreateFileInStore(formFile.FileName);

		    formFile.CopyTo(storeFile);
		    storeFile.Close();

		    return RedirectToAction("Index");
	    }
	    private Stream CreateFileInStore(string inputFileName)
	    {
		    var fileStore = Path.Combine(hostingEnvironment.ContentRootPath, "FileStore");

		    if (!Directory.Exists(fileStore))
			    Directory.CreateDirectory(fileStore);

		    var fileName = Path.GetFileName(inputFileName);
		    var storeFileName = Path.Combine(fileStore, $"{Guid.NewGuid()}_{fileName}");

		    logger.LogInformation($"File {fileName} stored as {storeFileName}");

			var storeFile = new FileStream(storeFileName, FileMode.Create);
		    return storeFile;
	    }
	}
}