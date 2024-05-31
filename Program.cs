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
		string folderPath = @"R:\QueryAnalyser\files"; // Update with the actual folder path
		string outputPath = @"R:\QueryAnalyser\files\indexes.csv"; // Update with the desired output file path

		var uniqueIndexes = new HashSet<string>();

		foreach (var file in Directory.GetFiles(folderPath, "*.csv"))
		{
			var urls = ExtractUrlsFromCsv(file);
			foreach (var url in urls)
			{
				var oDataFields = GenerateIndexesFromUrl(url);
				uniqueIndexes.Add(string.Join("_", oDataFields));
			}
		}

		WriteIndexesToCsv(outputPath, uniqueIndexes);
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

		// Matches any word that is followed by a space and an operator
		Regex fieldRegex = new(@"(\w+)(?=\s+(eq|ne|gt|ge|lt|le|has|in|startswith|endswith|contains)\s)", RegexOptions.IgnoreCase);

		foreach (var param in oDataQueryParams)
		{
			if (param.Key == "$filter" || param.Key == "$orderby")
			{
				MatchCollection matches = fieldRegex.Matches(param.Value);
				foreach (Match match in matches)
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
