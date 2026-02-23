using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Obrc.Input.Implementations;

public class VersionTwo
{
    private readonly string _weatherStationsFile;
    private readonly string _mesurementsFile;
    private readonly int _uniqueCities;
    private readonly int _totalCities;
    private readonly int _tenPercentDivisor;

    public VersionTwo(string weatherStationsFile, string mesurementsFile, int uniqueCities, int totalCities, int tenPercentDivisor)
    {
        _weatherStationsFile = weatherStationsFile;
        _mesurementsFile = mesurementsFile;
        _uniqueCities = uniqueCities;
        _totalCities = totalCities;
        _tenPercentDivisor = tenPercentDivisor;
    }

    public void SeedData()
    {
        try
        {
            var documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var sw = new Stopwatch();
            sw.Start();
            Dictionary<string, float> stations = GetUniqueStations(documentsDir);
            var stationsList = stations.ToList();
            sw.Stop();
            Console.WriteLine($"Getting 10,000 unique stations took {sw.ElapsedMilliseconds} ms");
            sw.Reset();

            var measurementsPath = Path.Combine(documentsDir, _mesurementsFile);

            using (var file = File.OpenWrite(measurementsPath))
            {
                using (var stream = new StreamWriter(file))
                {
                    var sb = new StringBuilder();
                    sw.Start();
                    SeedInitialTenThousand(stationsList, stream);
                    sw.Stop();
                    Console.WriteLine($"Seeding initial 10,000 took {sw.ElapsedMilliseconds} ms");
                    sw.Reset();
                    sw.Start();
                    WriteRandomStations(stationsList, stream);
                    sw.Stop();
                    Console.WriteLine($"Writing the remaining random stations took {sw.ElapsedMilliseconds} ms");
                }
            }
            Console.WriteLine("Complete!");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return;
        }
    }

    public Dictionary<string, float> GetUniqueStations(string documentsDir)
    {
        try
        {
            var stations = new Dictionary<string, float>();
            var filePath = Path.Combine(documentsDir, _weatherStationsFile);
            using var file = File.OpenRead(filePath);
            using var stream = new StreamReader(file);
            var lineNo = 0;
            while (!stream.EndOfStream && lineNo < _uniqueCities)
            {
                var line = stream.ReadLine();
                var parts = line!.Split(';');
                if (stations.TryAdd(parts[0], float.Parse(parts[1])))
                    lineNo++;
            }
            return stations;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    public void SeedInitialTenThousand(List<KeyValuePair<string, float>> stations, StreamWriter stream)
    {
        try
        {
            Span<char> buffer = stackalloc char[32];
            var entries = CollectionsMarshal.AsSpan(stations);
            foreach (ref var entry in entries)
            {
                if (entry.Value.TryFormat(buffer, out int bytesWritten, "0.0"))
                {
                    stream.Write(entry.Key.AsSpan());
                    stream.Write(';');
                    stream.Write(buffer.Slice(0, bytesWritten));
                    stream.Write('\n');
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    private void WriteRandomStations(List<KeyValuePair<string, float>> stations, StreamWriter stream)
    {
        try
        {
            var rand = new Random();
            int completedPercentage = 0;
            Span<char> buffer = stackalloc char[16];
            Span<char> bucket = stackalloc char[8192];
            int bucketIdx = 0;
            for (int lineNo = _uniqueCities; lineNo < _totalCities; lineNo++)
            {
                if (lineNo % _tenPercentDivisor == 0)
                {
                    completedPercentage += 10;
                    Console.WriteLine($"{completedPercentage}% completed");
                }

                var randIdx = rand.Next(0, stations.Count);
                var randJitter = rand.NextDouble() * 20 - 10;
                var station = stations[randIdx];
                var temperature = station.Value + randJitter;
                if (temperature.TryFormat(buffer, out int bytesWritten, "0.0"))
                {
                    // Get the length of the station line you are trying to write
                    // Station + ';' + bytesWritten + '\n'
                    int lineLength = station.Key.Length + bytesWritten + 2;
                    if (bucketIdx + lineLength > bucket.Length)
                    {
                        // write the bucket to the stream and reset the bucket
                        stream.Write(bucket.Slice(0, bucketIdx));
                        bucket.Clear();
                        bucketIdx = 0;
                    }
                    // add station
                    station.Key.AsSpan().CopyTo(bucket.Slice(bucketIdx));
                    bucketIdx += station.Key.Length;
                    // add ';'
                    bucket[bucketIdx] = ';';
                    bucketIdx++;
                    // add temperature
                    buffer.Slice(0, bytesWritten).CopyTo(bucket.Slice(bucketIdx));
                    bucketIdx += bytesWritten;
                    // add '\n'
                    bucket[bucketIdx] = '\n';
                    bucketIdx++;
                }
            }

            // Write the remaining bucket to the stream
            if (bucketIdx > 0)
                stream.Write(bucket.Slice(0, bucketIdx));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }
}


