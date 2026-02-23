
using System.Diagnostics;

namespace Obrc.Input.Implementations;

public class VersionOne
{
    private readonly string _weatherStationsFile;
    private readonly string _mesurementsFile;
    private readonly int _uniqueCities;
    private readonly int _totalCities;

    public VersionOne(string weatherStationsFile, string mesurementsFile, int uniqueCities, int totalCities)
    {
        _weatherStationsFile = weatherStationsFile;
        _mesurementsFile = mesurementsFile;
        _uniqueCities = uniqueCities;
        _totalCities = totalCities;
    }

    public void SeedData()
    {
        try
        {
            var sw = new Stopwatch();
            sw.Start();
            Dictionary<string, float> stations = new();
            var documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var filePath = Path.Combine(documentsDir, _weatherStationsFile);
            Console.WriteLine("Reading data...");

            using (var file = File.OpenRead(filePath))
            {
                var stream = new StreamReader(file);
                var lineNo = 0;
                while (!stream.EndOfStream && lineNo < _uniqueCities)
                {
                    var line = stream.ReadLine();
                    var parts = line!.Split(';');
                    stations.TryAdd(parts[0], float.Parse(parts[1]));
                    lineNo++;
                }
                stream.Close();
            }

            var measurementsPath = Path.Combine(documentsDir, _mesurementsFile);

            using (var file = File.OpenWrite(measurementsPath))
            {
                using (var stream = new StreamWriter(file))
                {
                    Console.WriteLine("Seeding data...");
                    // Write the first 10_000 stations
                    foreach (var station in stations)
                    {
                        stream.Write(station);
                        stream.Write(';');
                        stream.Write(station.Value);
                        stream.Write('\n');
                    }

                    var rand = new Random();
                    var stationsList = stations.ToList();
                    for (int lineNo = _uniqueCities; lineNo < _totalCities; lineNo++)
                    {
                        var randIdx = rand.Next(0, stations.Count);
                        var randJitter = rand.NextDouble() * 20 - 10;
                        var station = stationsList[randIdx];
                        stream.Write(station.Key);
                        stream.Write(';');
                        stream.Write((station.Value + randJitter).ToString("0.0"));
                        stream.Write('\n');
                    }
                }
            }
            sw.Stop();
            Console.WriteLine($"Total time taken {sw.ElapsedMilliseconds} ms");
            Console.WriteLine("Complete!");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return;
        }
    }
}


