using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureWorkshop
{
	public class Startup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddMvc();
		}

		private readonly IConfiguration configuration;

		public Startup(IConfiguration configuration)
		{
			this.configuration = configuration;
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime applicationLifetime)
		{
			applicationLifetime.ApplicationStarted.Register(OnStarted);

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseMvcWithDefaultRoute();
		}

		private void OnStarted()
		{
			var connectionString = configuration.GetConnectionString("ImageStore");

			CloudStorageAccount account = null;
			CloudStorageAccount.TryParse(connectionString, out account);

			var blobClient = account.CreateCloudBlobClient();
			var imageContainer = blobClient.GetContainerReference("images");
			var imageBlobCreationTask = imageContainer.CreateIfNotExistsAsync();

			var tableClient = account.CreateCloudTableClient();
			var imageTable = tableClient.GetTableReference("images");
			var imageTableCreationTask = imageTable.CreateIfNotExistsAsync();

			var queueClient = account.CreateCloudQueueClient();
			var imgQueue = queueClient.GetQueueReference("imgprocessing");
			var imgQueueCreationTask = imgQueue.CreateIfNotExistsAsync();

			var thumbnailContainer = blobClient.GetContainerReference("thumbnails");
			var thumbnailBlobCreationTask = thumbnailContainer
				.CreateIfNotExistsAsync()
				.ContinueWith
				(
					x => {
						thumbnailContainer.SetPermissionsAsync(
							new BlobContainerPermissions
							{
								PublicAccess = BlobContainerPublicAccessType.Blob
							});
						return true;
					}
				);

			Task.WaitAll(imageBlobCreationTask, imageTableCreationTask,
				imgQueueCreationTask, thumbnailBlobCreationTask);
		}

	}
}
