namespace Obrc.Implementations;

public class Version1
{
    private readonly string _mesurementsFile;
    private readonly string _outputFile;

    public Version1(string mesurementsFile, string outputFile)
    {
        _mesurementsFile = mesurementsFile;
        _outputFile = outputFile;
    }

    public void Run()
    {
        // Read in the data from the file
        // Calculate the min, max and average for each city
        // Print the results sorted alphabetically
    }
}
