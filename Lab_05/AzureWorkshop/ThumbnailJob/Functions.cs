using Microsoft.Azure.WebJobs;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace ThumbnailJob
{
	public class Functions
	{
		// This function will get triggered/executed when a new message is written 
		// on an Azure Queue called queue.
		public static void ProcessQueueMessage([QueueTrigger("queue")] string message, TextWriter log)
		{
			log.WriteLine(message);
		}

		public static void ConvertImage(
			[QueueTrigger("imgprocessing")] string blobId,
			[Blob("images/{queueTrigger}", FileAccess.Read)] Stream inputStream,
			[Blob("thumbnails/{queueTrigger}", FileAccess.Write)] Stream outputStream)
		{
			using (Image<Rgba32> image = Image.Load(inputStream))
			{
				image.Mutate(i =>
					i.Resize(100, 100)
				);
				image.Save(outputStream, new PngEncoder());
			}
		}
	}
}
