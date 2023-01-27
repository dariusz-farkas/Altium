# Altium sorter
Repo contains test sorting application as a part of interview process. It has been built using External Merge sort thus it's capable of sorting large data sets.
Sort method consists of view steps:
1. partition data - file is being splitted into a number of files which can be then sorted in memory.
2. sort strategy - Each file is being sorted using default .NET sort algorithm (quick sort) in O(n logn) complexity
3. K-way merge - sorted files are being merged until only one file exists containing all of the data.

## Usage

Application supports 3 commands:

### Sort file
`sort` - sorts source file

#### Required arguments:
- file - the full path to source file

#### Usage
```
dotnet run --project .\Altium.TestTask.ConsoleApp.csproj sort --f C:\workspace\unsorted-file.txt
```

### Create file
`create` - Generate test unsorted file for the ease of testing

#### Required arguments:
- f - the full path to source file
- s - the size that consists of length followed by a unit (kb, mb, gb), eg. 250mb

#### Usage
```
dotnet run --project .\Altium.TestTask.ConsoleApp.csproj create --f C:\workspace\nsorted-file.txt --s 200mb
```

### Verify file
`verify` - Verifies size and result file correctness against the source 

#### Required arguments:
- u - the full path to unsorted file
- s - the full path to sorted file

#### Usage
```
dotnet run --project .\Altium.TestTask.ConsoleApp.csproj verify --u C:\workspace\sorted-file.txt --s C:\workspace\sorted-file.txt
```

## Configuration

Application is higly configurable. Changes are done in appsettings config file. Supported parameters:

### Partitioner
- Partition:FileSize - minimum size of the splitted file. Sorter will create as many partitioned files as required. You can control whether app should create more files with lower size or less files but bigger.
This has impact on the memory consumption because, those files are loaded into memory at stage 2 for sorting.
- Partition:BufferSize - The buffer size used for reading the source file. Has impact on memory consumption and overall performance.

### Sorter
- Sort:InputBufferSize - input buffer size used for reading unsorted (partitioned) files
- Sort:OutputBufferSize - output buffer size used for writing data to sorted files.
- Sort:MaxParallelism - maximum number of threads being used to sort files in parallel. Can speed up execution but it consumes N times more memory so must be used with caution.

### Merger
- Merge:ChunkSize - used for merging sorted files, it corresponds to a maximum number of files that will take part in single K-way merge operation
- Merge:MaxParallelism - maximum number of threads being used to perform K-way merge into one file. Has less footprint on memory as bytes are lazy loaded from files.

### FileSystem
- TempDir - optional parameter that indicates the temporary files directory. If not specified it will use the folder of the executing assembly location.

## Optimal settings

Based on tens of different configuration runs, given setup is the most performant:
- Partition:FileSize: 16777216
- Partition:BufferSize: 65536
- Sort:MaxParallelism = 75% cpu
- Merge:MaxParallelism = 75% cpu
- Merge:ChunkSize = 4

However, it has the most memory footprint ~1.5 GB