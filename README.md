
# QueryAnalyser

A utility tool for analyzing OData queries in CSV files to identify potential MongoDB indexes.

## Overview

QueryAnalyser extracts field names from OData $filter and $orderby operations in URLs contained in CSV files. It processes these field names to generate a list of potential MongoDB indexes that would optimize query performance.

## Requirements

- .NET 7.0 or higher
- CSV files containing a column named "url" with OData query URLs

## Installation

1. Clone this repository
2. Build the solution: `dotnet build`
3. Run the application from the bin directory or using `dotnet run`

## Usage

```
QueryAnalyser.exe [searchPattern] [folderPath] [outputPath]
```

### Parameters

- `searchPattern` (optional): File pattern to search for (default: "query*.csv")
- `folderPath` (optional): Path to folder containing CSV files (default: "./files")
- `outputPath` (optional): Path for the output file (default: "[folderPath]/indexes.csv")

### Examples

```
# Use default settings
QueryAnalyser.exe

# Process all CSV files
QueryAnalyser.exe "*.csv"

# Process files with specific pattern
QueryAnalyser.exe "venue*.csv"

# Specify input and output locations
QueryAnalyser.exe "query*.csv" "C:/Data/CSVFiles" "C:/Results/recommended_indexes.csv"
```

## Input Format

The tool expects CSV files with a column named "url" containing OData query URLs. Example URL format:

```
https://api.example.com/data?$filter=tolower(title) eq '' and tolower(address) eq 'paraguay'&$orderby=createdDate desc
```

## Output

The tool generates a CSV file listing potential MongoDB indexes based on field usage in the analyzed queries:

```
Indexes
title_address
createdDate
status_updatedDate
```

Each line represents a combination of fields that are frequently used together in queries and would benefit from a compound index.

## Features

- Extracts field names both directly and from within OData functions (tolower, toupper, etc.)
- Handles common OData operators (eq, ne, gt, ge, lt, le, has, startswith, endswith, contains)
- Provides progress tracking via console output
- Eliminates duplicate index combinations

## Limitations

- Only processes $filter and $orderby OData operators
- Complex expressions may require additional parsing logic

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.
