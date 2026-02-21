using System.Diagnostics;

public class Program
{
    const string WEATHER_STATIONS_FILE = "weather_stations.txt";
    const string MEASUREMENTS_FILE = "measurements.txt";
    const int UNIQUE_CITIES = 10_000;
    const int TOTAL_CITIES = 1_000_000;
    const int TEN_PERCENT_DIVISOR = 100_000;

    public static void Main(string[] args)
    {
        try
        {
            Dictionary<string, float> stations = new();
            var documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var filePath = Path.Combine(documentsDir, WEATHER_STATIONS_FILE);
            Stopwatch sw = new();
            Console.WriteLine("Reading data...");
            sw.Start();

            using (var file = File.OpenRead(filePath))
            {
                var stream = new StreamReader(file);
                var lineNo = 0;
                while (!stream.EndOfStream && lineNo < UNIQUE_CITIES)
                {
                    var line = stream.ReadLine();
                    var parts = line!.Split(';');
                    stations.TryAdd(parts[0], float.Parse(parts[1]));
                    lineNo++;
                }
                stream.Close();
            }

            sw.Stop();
            Console.WriteLine($"Getting 10,000 unique cities took {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            sw.Start();
            var measurementsPath = Path.Combine(documentsDir, MEASUREMENTS_FILE);

            using (var file = File.OpenWrite(measurementsPath))
            {
                using (var stream = new StreamWriter(file))
                {
                    Console.WriteLine("Seeding data...");
                    // Write the first 10_000 stations
                    foreach (var station in stations)
                        stream.WriteLine($"{station.Key};{station.Value}");
                    Console.WriteLine($"Writing the first 10,000 lines took {sw.ElapsedMilliseconds} ms");

                    var rand = new Random();
                    var stationsList = stations.ToList();
                    int completedPercentage = 0;
                    for (int lineNo = UNIQUE_CITIES; lineNo < TOTAL_CITIES; lineNo++)
                    {
                        if (lineNo % TEN_PERCENT_DIVISOR == 0)
                        {
                            completedPercentage += 10;
                            Console.WriteLine($"{completedPercentage}% completed");
                        }

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
            Console.WriteLine($"Total elapsed time: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"On 1 billion records, it would take {(sw.ElapsedMilliseconds * 1000) / 1000 / 60} minutes to complete");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return;
        }
    }
}


