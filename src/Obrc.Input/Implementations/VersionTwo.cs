using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Obrc.Input.Implementations;

public class VersionTwo
{
    const string WEATHER_STATIONS_FILE = "weather_stations.txt";
    const string MEASUREMENTS_FILE = "measurements.txt";
    const int UNIQUE_CITIES = 10_000;
    const int TOTAL_CITIES = 1_000_000_000;
    const int TEN_PERCENT_DIVISOR = 100_000_000;

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

            var measurementsPath = Path.Combine(documentsDir, MEASUREMENTS_FILE);

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
            var filePath = Path.Combine(documentsDir, WEATHER_STATIONS_FILE);
            using var file = File.OpenRead(filePath);
            using var stream = new StreamReader(file);
            var lineNo = 0;
            while (!stream.EndOfStream && lineNo < UNIQUE_CITIES)
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
        var rand = new Random();
        int completedPercentage = 0;
        Span<char> buffer = stackalloc char[16];
        Span<char> bucket = stackalloc char[4096];
        int bucketIdx = 0;
        for (int lineNo = UNIQUE_CITIES; lineNo < TOTAL_CITIES; lineNo++)
        {
            if (lineNo % TEN_PERCENT_DIVISOR == 0)
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
                // If there is space in the bucket, write the station line to the bucket
                if (bucketIdx + lineLength < bucket.Length)
                {
                    // write station
                    station.Key.AsSpan().CopyTo(bucket.Slice(bucketIdx));
                    bucketIdx += station.Key.Length;
                    // write ';'
                    bucket[bucketIdx] = ';';
                    bucketIdx++;
                    // write temperature
                    buffer.Slice(0, bytesWritten).CopyTo(bucket.Slice(bucketIdx));
                    bucketIdx += bytesWritten;
                    // write '\n'
                    bucket[bucketIdx] = '\n';
                    bucketIdx++;
                }
                else
                {
                    // write the bucket to the stream and reset the bucket
                    // and write the line you are currently trying to add to the bucket 
                    stream.Write(bucket.Slice(0, bucketIdx));
                    bucket.Clear();
                    bucketIdx = 0;
                    station.Key.AsSpan().CopyTo(bucket.Slice(bucketIdx));
                    bucketIdx += station.Key.Length;
                    bucket[bucketIdx] = ';';
                    bucketIdx++;
                    buffer.Slice(0, bytesWritten).CopyTo(bucket.Slice(bucketIdx));
                    bucketIdx += bytesWritten;
                    bucket[bucketIdx] = '\n';
                    bucketIdx++;
                }
            }
        }
    }
}


