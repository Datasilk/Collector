using System;
using System.Diagnostics;
using Collector.Common;
using Collector.Common.Entities;
using Collector.Data.Interfaces;
using Collector.Web.Server.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Collector.Web.Server.Workers
{
	public class VideoWorker : IWorker
	{
		private readonly IJournalVideosRepository _videoRepo;
		private readonly ILogger<VideoWorker> _logger;
		private readonly IHubContext<WorkerHub> _hubContext;
		private readonly object _stateLock = new();
		private int _currentProgress = 0;
		private string _currentStatus = "";
		private string _title = "";

		public VideoWorker(IJournalVideosRepository videoRepo, ILogger<VideoWorker> logger, IHubContext<WorkerHub> hubContext)
		{
			_videoRepo = videoRepo;
			_logger = logger;
			_hubContext = hubContext;
		}

		public Task Stop()
		{
			return Task.CompletedTask;
		}

		public async Task Progress(string appUserId, Guid workerId)
		{
			int progress;
			string status;
			string title;
			lock (_stateLock)
			{
				progress = _currentProgress;
				status = _currentStatus;
				title = _title;
			}

			await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress, status, title }, CancellationToken.None);
		}

		public async Task DownloadVideo(string appUserId, Guid workerId, string url, int journalId, string entryId, string moduleId, CancellationToken cancellationToken)
		{
			try
			{
				if (!IsYouTubeUrl(url))
				{
					_logger.LogWarning("VideoWorker only supports YouTube URLs at this time. Url={Url}", url);
					return;
				}

				var existingVideo = await _videoRepo.GetByUrl(url);

				int videoId;
				string fileName;
				string relativePath;
				string title;

				if (existingVideo != null)
				{
					videoId = existingVideo.Id;
					fileName = existingVideo.Filename;
					relativePath = Path.Combine(existingVideo.JournalEntryId.ToString(), fileName);
					title = existingVideo.Title;
					lock (_stateLock)
					{
						_title = title;
					}

					if (existingVideo.Downloaded)
					{
						_logger.LogInformation("Video {VideoId} already downloaded, skipping download in VideoWorker", videoId);
						// Notify client that the video is already downloaded
						lock (_stateLock)
						{
							_currentProgress = 100;
							_currentStatus = "Already downloaded";
						}
						await SendWorkerMessage(appUserId, workerId, "DownloadComplete", new
						{
							id = videoId,
							videoPath = relativePath.Replace("\\", "/"),
							thumbnailPath = (string?)null,
							width = existingVideo.Width,
							height = existingVideo.Height,
							duration = existingVideo.Duration,
							entryId = existingVideo.JournalEntryId.ToString()
						}, cancellationToken);
						await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = 100, status = _currentStatus }, cancellationToken);
						return;
					}
				}
				else
				{
					var originalTitle = await GetVideoTitle(url, cancellationToken);
					title = originalTitle?.Length > 128 ? originalTitle.Substring(0, 128) : originalTitle ?? string.Empty;
					_title = title;
					await SendWorkerMessage(appUserId, workerId, "DownloadTitle", new { title }, cancellationToken);

					fileName = $"{Guid.NewGuid()}.mp4";
					relativePath = Path.Combine(entryId, fileName);

					var video = new JournalVideo
					{
						JournalId = journalId,
						JournalEntryId = Guid.Parse(entryId),
						ModuleId = moduleId,
						Filename = fileName,
						OriginalFilename = string.Empty,
						Url = url,
						Downloaded = false,
						Duration = 0,
						Width = 0,
						Height = 0,
						Metadata = string.Empty,
						Title = title,
						Description = string.Empty
					};

					videoId = await _videoRepo.Add(video);
					// Inform client about the new video record
					await SendWorkerMessage(appUserId, workerId, "VideoRecordCreated", new
					{
						id = videoId,
						videoPath = relativePath.Replace("\\", "/"),
						title
					}, cancellationToken);
				}

				var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), relativePath);

				var directory = Path.GetDirectoryName(videoFullPath);
				if (!Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}

				lock (_stateLock)
				{
					_currentProgress = 0;
					_currentStatus = "Downloading video...";
				}
				await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, cancellationToken);

				await RunYtDlpDownload(url, videoFullPath, cancellationToken, appUserId, workerId);

				if (!File.Exists(videoFullPath))
				{
					_logger.LogError("Video file not found after yt-dlp for {Path}", videoFullPath);
					lock (_stateLock)
					{
						_currentProgress = 0;
						_currentStatus = "Video file not found on server after download.";
					}
					await SendWorkerMessage(appUserId, workerId, "DownloadError", new { message = _currentStatus }, cancellationToken);
					return;
				}

				lock (_stateLock)
				{
					_currentProgress = 80;
					_currentStatus = "Generating preview thumbnail...";
				}
				await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, cancellationToken);

				var thumbnailFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_thumb.jpg";
				var thumbnailRelativePath = Path.Combine(entryId, thumbnailFileName);
				var thumbnailSuccess = await GenerateThumbnail(relativePath, thumbnailRelativePath, url, cancellationToken: cancellationToken);

				lock (_stateLock)
				{
					_currentProgress = 90;
					_currentStatus = "Processing video metadata...";
				}
				await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, cancellationToken);

				var (width, height, duration) = await GetVideoMetadata(relativePath, cancellationToken);

				decimal fileSizeMb = 0;
				try
				{
					var fileInfo = new FileInfo(videoFullPath);
					if (fileInfo.Exists)
					{
						fileSizeMb = Math.Round((decimal)fileInfo.Length / (1024 * 1024), 2);
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to get video file size for {VideoPath}", videoFullPath);
				}

				lock (_stateLock)
				{
					_currentProgress = 94;
					_currentStatus = "Generating seek preview thumbnails...";
				}
				await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, cancellationToken);

				await GenerateSeekPreviewThumbnails(relativePath, cancellationToken);

				await _videoRepo.UpdateDownloaded(videoId, true, fileName, duration, width, height, fileSizeMb);
				_logger.LogInformation("Video {VideoId} download completed in VideoWorker", videoId);
				await SendWorkerMessage(appUserId, workerId, "DownloadComplete", new
				{
					id = videoId,
					videoPath = relativePath.Replace("\\", "/"),
					thumbnailPath = thumbnailSuccess ? thumbnailRelativePath.Replace("\\", "/") : null,
					width,
					height,
					duration,
					entryId
				}, cancellationToken);
				lock (_stateLock)
				{
					_currentProgress = 100;
					_currentStatus = "Complete!";
				}
				await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress, status = _currentStatus }, cancellationToken);
			}
			catch (OperationCanceledException)
			{
				_logger.LogInformation("VideoWorker download cancelled for URL {Url}", url);
				lock (_stateLock)
				{
					_currentProgress = 0;
					_currentStatus = "Download cancelled.";
				}
				await SendWorkerMessage(appUserId, workerId, "DownloadError", new { message = _currentStatus }, CancellationToken.None);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "VideoWorker DownloadVideo error for URL {Url}", url);
				lock (_stateLock)
				{
					_currentProgress = 0;
					_currentStatus = ex.Message;
				}
				await SendWorkerMessage(appUserId, workerId, "DownloadError", new { message = _currentStatus }, CancellationToken.None);
			}
		}

		private Task SendWorkerMessage(string appUserId, Guid workerId, string eventName, object payload, CancellationToken cancellationToken)
		{
			// Broadcast to all clients; WorkerHubProvider will filter by AppUserId and WorkerId
			return _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, eventName, payload, cancellationToken);
		}

		private bool IsYouTubeUrl(string url)
		{
			return url.Contains("youtube.com") || url.Contains("youtu.be");
		}

		private async Task<string> GetVideoTitle(string url, CancellationToken cancellationToken)
		{
			try
			{
				var startInfo = new ProcessStartInfo
				{
					FileName = "yt-dlp",
					Arguments = $"--get-title \"{url}\"",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				};

				using (var process = Process.Start(startInfo))
				{
					if (process == null) return "Downloaded Video";

					var title = await process.StandardOutput.ReadToEndAsync();
					await process.WaitForExitAsync(cancellationToken);
					return title.Trim();
				}
			}
			catch
			{
				return "Downloaded Video";
			}
		}

		private async Task RunYtDlpDownload(string url, string videoFullPath, CancellationToken cancellationToken, string appUserId, Guid workerId)
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = "yt-dlp",
				Arguments = $"-f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best\" --merge-output-format mp4 --no-keep-video -o \"{videoFullPath}\" \"{url}\"",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using (var process = Process.Start(startInfo))
			{
				if (process == null)
				{
					throw new InvalidOperationException("Failed to start yt-dlp process");
				}

				// Track progress by reading yt-dlp output
				process.OutputDataReceived += async (sender, e) =>
				{
					if (string.IsNullOrEmpty(e.Data)) return;
					try
					{
						if (e.Data.Contains("%"))
						{
							var percentIndex = e.Data.IndexOf("%", StringComparison.Ordinal);
							if (percentIndex > 0)
							{
								var percentStart = percentIndex - 1;
								while (percentStart > 0 && (char.IsDigit(e.Data[percentStart - 1]) || e.Data[percentStart - 1] == '.'))
								{
									percentStart--;
								}
								if (double.TryParse(e.Data.Substring(percentStart, percentIndex - percentStart), out double progress))
								{
									int mappedProgress;
									string status;
									if (progress < 100)
									{
										mappedProgress = (int)Math.Round((90f / 100f) * progress);
										status = "Downloading video...";
									}
									else
									{
										mappedProgress = 90;
										status = "Processing video & audio data...";
									}
									lock (_stateLock)
									{
										_currentProgress = mappedProgress;
										_currentStatus = status;
									}
									await SendWorkerMessage(appUserId, workerId, "DownloadProgress", new { progress = _currentProgress }, CancellationToken.None);
								}
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error parsing yt-dlp progress output: {Line}", e.Data);
					}
				};

				process.ErrorDataReceived += (sender, e) =>
				{
					if (!string.IsNullOrEmpty(e.Data))
					{
						Console.WriteLine($"yt-dlp error: {e.Data}");
						_logger.LogError("yt-dlp error: {Error}", e.Data);
					}
				};

				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
				await process.WaitForExitAsync(cancellationToken);

				if (process.ExitCode != 0)
				{
					var error = await process.StandardError.ReadToEndAsync();
					throw new InvalidOperationException($"yt-dlp failed with code {process.ExitCode}: {error}");
				}
			}
		}

		private async Task<bool> GenerateThumbnail(string videoRelativePath, string thumbnailRelativePath, string videoUrl = null, int width = 0, int height = 0, bool crop = false, int seekSeconds = 1, CancellationToken cancellationToken = default)
		{
			var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), videoRelativePath);
			var thumbnailFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), thumbnailRelativePath);

			var success = await Common.Videos.GenerateThumbnail(videoFullPath, thumbnailFullPath, videoUrl, width, height, crop, seekSeconds, timeoutSeconds: 30);

			if (!success)
			{
				_logger.LogError("Failed to generate thumbnail for {VideoPath}", videoRelativePath);
			}
			else if (!crop && !string.IsNullOrEmpty(videoUrl))
			{
				_logger.LogInformation("Successfully generated thumbnail for {VideoPath}", videoRelativePath);
			}

			return success;
		}

		private async Task<(int width, int height, int duration)> GetVideoMetadata(string videoRelativePath, CancellationToken cancellationToken)
		{
			var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), videoRelativePath);
			return await Common.Videos.GetVideoMetadata(videoFullPath);
		}

		private async Task GenerateSeekPreviewThumbnails(string videoRelativePath, CancellationToken cancellationToken)
		{
			try
			{
				var videoFullPath = Path.Combine(Files.GetPath(Files.Paths.Videos), videoRelativePath);

				if (!File.Exists(videoFullPath))
				{
					_logger.LogError("Video file not found for seek preview generation: {VideoPath}", videoFullPath);
					return;
				}

				var (width, height, duration) = await GetVideoMetadata(videoRelativePath, cancellationToken);

				if (duration <= 0)
				{
					_logger.LogWarning("Invalid video duration for seek preview generation");
					return;
				}

				var videoDirectory = Path.GetDirectoryName(videoFullPath);
				var videoFileNameWithoutExt = Path.GetFileNameWithoutExtension(videoFullPath);
				var previewFolder = Path.Combine(videoDirectory, videoFileNameWithoutExt);

				if (!Directory.Exists(previewFolder))
				{
					Directory.CreateDirectory(previewFolder);
				}

				for (int second = 0; second < duration; second += 10)
				{
					cancellationToken.ThrowIfCancellationRequested();

					var previewFileName = $"preview_{second}.jpg";
					var previewRelativePath = Path.Combine(Path.GetDirectoryName(videoRelativePath), videoFileNameWithoutExt, previewFileName);

					try
					{
						var previewDirectory = Path.GetDirectoryName(Path.Combine(Files.GetPath(Files.Paths.Videos), previewRelativePath));
						if (!string.IsNullOrEmpty(previewDirectory) && !Directory.Exists(previewDirectory))
						{
							Directory.CreateDirectory(previewDirectory);
						}
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Failed to create directory for seek preview thumbnail at {Second}s", second);
						continue;
					}

					await GenerateThumbnail(videoRelativePath, previewRelativePath, null, 160, 90, true, second, cancellationToken);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Exception during seek preview thumbnail generation in VideoWorker");
			}
		}
	}
}