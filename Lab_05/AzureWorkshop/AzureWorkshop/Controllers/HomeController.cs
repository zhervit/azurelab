using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureWorkshop.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureWorkshop.Controllers
{
    public class HomeController : Controller
    {
	    private readonly IHostingEnvironment hostingEnvironment;
	    private readonly ILogger<HomeController> logger;
	    private readonly IConfiguration configuration;

		public HomeController(IHostingEnvironment hostingEnvironment, ILogger<HomeController> logger, IConfiguration configuration)
		{
		    this.hostingEnvironment = hostingEnvironment;
		    this.logger = logger;
			this.configuration = configuration;
		}

		public IActionResult Index()
        {
            return View();
        }

	    public IActionResult List()
	    {
		    var savedFiles = GetSavedFilesInfo();

		    return View(savedFiles);
	    }

	    private IEnumerable<FileEntity> GetSavedFilesInfo()
	    {
			var result = new List<FileEntity>();

		    var connectionString = configuration.GetConnectionString("ImageStore");

		    CloudStorageAccount account = null;
		    CloudStorageAccount.TryParse(connectionString, out account);

		    var tableClient = account.CreateCloudTableClient();
		    var imageTable = tableClient.GetTableReference("images");

		    var blobClient = account.CreateCloudBlobClient();
		    var thumbnailsContainer = blobClient.GetContainerReference("thumbnails");

		    var query = new TableQuery<FileEntity>().Where("ClientAddress ne '::1'");

			TableContinuationToken token = null;
		    do
		    {
			    var t = imageTable.ExecuteQuerySegmentedAsync(query, token).Result;

			    t.Results.ForEach(i =>
			    {
				    var blob = thumbnailsContainer.GetBlobReference(i.RowKey);
				    if (blob.ExistsAsync().Result)
				    {
					    i.Url = blob.Uri.ToString();
				    }
			    });

			    result.AddRange(t.Results);

			    token = t.ContinuationToken;

		    } while (token != null);


		    return result;
		}

		[HttpPost]
	    public IActionResult Upload(IFormFile formFile)
	    {
			SaveFile(Path.GetFileName(formFile.FileName), formFile.OpenReadStream(), Request.HttpContext.Connection.RemoteIpAddress.ToString());

			return RedirectToAction("Index");
	    }

		private void SaveFile(string name, Stream stream, string address)
		{
			var connectionString = configuration.GetConnectionString("ImageStore");

			CloudStorageAccount account = null;
			CloudStorageAccount.TryParse(connectionString, out account);

			var blobClient = account.CreateCloudBlobClient();
			var imageContainer = blobClient.GetContainerReference("images");
			var blobId = Guid.NewGuid().ToString();
			var blob = imageContainer.GetBlockBlobReference(blobId);

			var result1 = blob.UploadFromStreamAsync(stream);

			var tableClient = account.CreateCloudTableClient();
			var imageTable = tableClient.GetTableReference("images");

			var newFileEntity = new FileEntity(blobId)
			{
				Name = name,
				ClientAddress = address
			};

			var result2 = imageTable.ExecuteAsync(TableOperation.Insert(newFileEntity));

			Task.WaitAll(result1, result2);

			var queueClient = account.CreateCloudQueueClient();
			var imgQueue = queueClient.GetQueueReference("imgprocessing");
			imgQueue.AddMessageAsync(new CloudQueueMessage(blobId)).Wait();
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