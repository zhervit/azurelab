using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureWorkshop.Models
{
	public class FileEntity : TableEntity
	{
		public FileEntity() { }

		public FileEntity(string blobId)
			: base("Images", blobId) { }

		public string Name { get; set; }
		public string ClientAddress { get; set; }
		public string Url { get; set; }
	}

}
