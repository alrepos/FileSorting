# FileSorting

## Description

VS Solution with "FileSorter" C# console application to sort text data from an input file and save the ordered output to a new file. There is also the "FileGenerator" application for generating an input file to test the sorting app logic. The solution is implemented according to the Domain-driven design (DDD) pattern, and its logic is optimized for large input and output file sizes (with gigabytes or terabytes of data).

## Usage

### FileGenerator

Run the "FileGenerator" app to generate an input file. File size and path can be passed to the parameters of the main "StartGenerating" method to override default values (0.3 Gb and public "Documents" folder).

### FileSorter

Run the "FileSorter" app to sort text data from an input file and save the ordered output to a new file. File size, input and output paths can be passed to the parameters of the main "StartSortingAsync" method to override default values (0.3 Gb and public "Documents" folder).

### BenchmarkSuite

Run the "BenchmarkSuite" app to perform benchmarks for other applications.
