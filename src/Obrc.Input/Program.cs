using Obrc.Input.Implementations;

public class Program
{
    const string WEATHER_STATIONS_FILE = "weather_stations.txt";
    const string MEASUREMENTS_FILE = "measurements.txt";
    const int UNIQUE_CITIES = 10_000;
    const int TOTAL_CITIES = 1_000_000_000;


    public async static Task Main(string[] args)
    {
        var impl = new VersionThree(WEATHER_STATIONS_FILE, MEASUREMENTS_FILE, UNIQUE_CITIES, TOTAL_CITIES);
        await impl.SeedData();
    }
}


