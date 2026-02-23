using Obrc.Implementations;

public class Program
{
    const string MEASUREMENTS_FILE = "measurements.txt";
    const string OUTPUT_FILE = "output.txt";
    public static void Main(string[] args)
    {
        var impl = new Version1(MEASUREMENTS_FILE, OUTPUT_FILE);
        impl.Run();
    }
}
