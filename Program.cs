using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Specialized;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;

class Program
{
	static void Main(string[] args)
	{
		// Get the directory of the executing assembly (bin folder)
		string binDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
		// Navigate up two levels to reach the project root
		string projectRoot = Directory.GetParent(binDirectory).Parent.Parent.FullName;
		
		// Default values
		string searchPattern = "query*.csv";
		string folderPath = Path.Combine(projectRoot, "files");
		string outputPath;
		
		// Parse command line arguments
		if (args.Length > 0)
		{
			searchPattern = args[0];
		}
		
		if (args.Length > 1)
		{
			folderPath = args[1];
		}
		
		if (args.Length > 2)
		{
			outputPath = args[2];
		}
		else
		{
			outputPath = Path.Combine(folderPath, "indexes.csv");
		}
		
		var uniqueIndexes = new HashSet<string>();

		// At the beginning
		Console.WriteLine($"Starting application...");
		Console.WriteLine($"Using folder path: {folderPath}");
		Console.WriteLine($"Output will be saved to: {outputPath}");
		Console.WriteLine($"Searching for files matching pattern: {searchPattern}");

		// Before processing files
		if (!Directory.Exists(folderPath))
		{
			Console.WriteLine($"Error: Directory '{folderPath}' does not exist.");
			return;
		}
		
		string[] filesToProcess = Directory.GetFiles(folderPath, searchPattern);
		Console.WriteLine($"Found {filesToProcess.Length} files to process");
		
		if (filesToProcess.Length == 0)
		{
			Console.WriteLine("No files found matching the search pattern.");
			return;
		}

		// Process each file
		int fileCounter = 0;
		foreach (var file in filesToProcess)
		{
			fileCounter++;
			Console.WriteLine($"Processing file {fileCounter}/{filesToProcess.Length}: {Path.GetFileName(file)}");
			
			var urls = ExtractUrlsFromCsv(file);
			Console.WriteLine($"  - Extracted {urls.Count} URLs from file");
			
			int urlCounter = 0;
			foreach (var url in urls)
			{
				urlCounter++;
				if (urlCounter % 1000 == 0)
				{
					Console.WriteLine($"  - Processed {urlCounter}/{urls.Count} URLs");
				}
				
				var oDataFields = GenerateIndexesFromUrl(url);
				uniqueIndexes.Add(string.Join("_", oDataFields));
			}
		}

		// Final status
		Console.WriteLine($"Processing complete. Found {uniqueIndexes.Count} unique index combinations");
		Console.WriteLine($"Writing results to {outputPath}");

		// Create directory for output file if it doesn't exist
		Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

		WriteIndexesToCsv(outputPath, uniqueIndexes);

		// After writing to file
		Console.WriteLine("Operation completed successfully");
	}

	static List<string> ExtractUrlsFromCsv(string filePath)
	{
		var urls = new List<string>();
		var config = new CsvConfiguration(CultureInfo.InvariantCulture)
		{
			MissingFieldFound = null,
			HeaderValidated = null,
		};

		using (var reader = new StreamReader(filePath))
		using (var csv = new CsvReader(reader, config))
		{
			csv.Read();
			csv.ReadHeader();
			int urlIndex = csv.Context.Reader.HeaderRecord.ToList().IndexOf("url");

			if (urlIndex == -1) return urls;

			while (csv.Read())
			{
				var url = csv.GetField(urlIndex);
				if (!string.IsNullOrEmpty(url))
				{
					urls.Add(url);
				}
			}
		}
		return urls;
	}

	public static List<string> GenerateIndexesFromUrl(string url)
	{
		Uri uri = new(url);
		string queryString = uri.Query;

		NameValueCollection queryParams = HttpUtility.ParseQueryString(queryString);

		KeyValuePair<string, string>[] oDataQueryParams = queryParams.AllKeys
										.Where(kvp => kvp is not null && kvp.StartsWith("$"))
										.Select(key => new KeyValuePair<string, string>(key, queryParams[key]))
										.ToArray();

		List<string> fieldNames = [];

		// Matches direct field names followed by operators
		Regex directFieldRegex = new(@"(\w+)(?=\s+(eq|ne|gt|ge|lt|le|has|in|startswith|endswith|contains)\s)", RegexOptions.IgnoreCase);
		
		// Matches field names inside function calls like tolower(fieldName)
		Regex functionFieldRegex = new(@"(?:tolower|toupper|trim|substring|concat|year|month|day|hour|minute|second|round|floor|ceiling|cast)\((\w+)\)", RegexOptions.IgnoreCase);

		foreach (var param in oDataQueryParams)
		{
			if (param.Key == "$filter" || param.Key == "$orderby")
			{
				// Process direct field names
				MatchCollection directMatches = directFieldRegex.Matches(param.Value);
				foreach (Match match in directMatches)
				{
					string fieldName = match.Groups[1].Value;
					if (!fieldNames.Contains(fieldName))
					{
						fieldNames.Add(fieldName);
					}
				}
				
				// Process field names inside functions
				MatchCollection functionMatches = functionFieldRegex.Matches(param.Value);
				foreach (Match match in functionMatches)
				{
					string fieldName = match.Groups[1].Value;
					if (!fieldNames.Contains(fieldName))
					{
						fieldNames.Add(fieldName);
					}
				}
			}
		}

		return fieldNames;
	}

	static void WriteIndexesToCsv(string filePath, HashSet<string> indexes)
	{
		using (var writer = new StreamWriter(filePath))
		{
			writer.WriteLine("Indexes");
			foreach (var index in indexes)
			{
				writer.WriteLine(index);
			}
		}
	}
}
