using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Id3;

namespace Mp3Renamer
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args == null)
				args = new string[0];

			var directory = args.FirstOrDefault();
			var removeBrackets = args.Any(p => p == "-rb");
			var addTrackNumber = args.Any(p => p == "-n");
			var addArtist = args.Any(p => p == "-a");
			var addAlbum = args.Any(p => p == "-al");
			var addTitle = args.Any(p => p == "-t");
			var includeSubfolders = args.Any(p => p == "-s");

			if (!addArtist && !addTitle && String.IsNullOrEmpty(directory))
			{
				Console.WriteLine("Invalid parameters provided. Please use: [Directory] -s (include subfolders) -rb (remove text in brackets) -n (add track number) -a (add artist) -al (add album) -t (add title)");
				return;
			}

			foreach (var musicFilePath in Directory.GetFiles(directory, "*.mp3", includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
			{
				var newFileName = String.Empty;

				try
				{
					using (var mp3 = new Mp3(musicFilePath))
					{
						var tag = mp3.GetTag(Id3TagFamily.Version2X);

						if (tag == null)
							throw new Exception("File is missing tags");

						if (addTrackNumber && tag.Track.TrackCount > 0)
							newFileName += tag.Track.Value.ToString($"D{tag.Track.Padding ?? 2}");

						if (addArtist)
						{
							if (String.IsNullOrEmpty(tag.Artists))
								throw new Exception("File is missing an Artist");

							newFileName += (!String.IsNullOrEmpty(newFileName) ? " - " : String.Empty) + tag.Artists;
						}

						if (addAlbum)
						{
							if (String.IsNullOrEmpty(tag.Album))
								throw new Exception("File is missing an Album");

							newFileName += (!String.IsNullOrEmpty(newFileName) ? " - " : String.Empty) + tag.Album;
						}

						if (addTitle)
						{
							if (String.IsNullOrEmpty(tag.Title))
								throw new Exception("File is missing a Title");

							newFileName += (!String.IsNullOrEmpty(newFileName) ? " - " : String.Empty) + tag.Title;
						}

						if (removeBrackets)
						{
							while (newFileName.Contains("[") || newFileName.Contains("("))
							{
								var bracketStart = newFileName.IndexOf('(');
								var bracketEnd = newFileName.IndexOf(')');

								if (bracketStart > 0)
									newFileName = newFileName.Remove(bracketStart, bracketEnd - bracketStart + 1);

								bracketStart = newFileName.IndexOf('[');
								bracketEnd = newFileName.IndexOf(']');

								if (bracketStart > 0)
									newFileName = newFileName.Remove(bracketStart, bracketEnd - bracketStart + 1);
							}
						}

						newFileName = newFileName.Trim().Replace("\0", String.Empty) + ".mp3";
					}

					var fileDir = Path.GetDirectoryName(musicFilePath);
					var newFilePath = Path.Combine(fileDir, newFileName);

					if (musicFilePath == newFilePath)
					{
						Console.ForegroundColor = ConsoleColor.DarkGray;
						Console.WriteLine($"Skipping [{musicFilePath}]");
						Console.ResetColor();
						continue;
					}

					File.Move(musicFilePath, newFilePath);
					Console.WriteLine($"Renamed [{musicFilePath}] ---> [{newFileName}]");
				}
				catch (Exception ex)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($"ERROR: Could not rename [{musicFilePath}] ---> [{newFileName}]: {ex.Message}");
					Console.ResetColor();
				}
			}
		}
	}
}
