using System.Buffers;
using System.Text;
using System.Threading.Channels;

namespace Obrc.Input.Implementations;

public record Bucket(byte[] data, int activeIdx);

public class VersionThree
{
    private readonly string _weatherStationsFile;
    private readonly string _mesurementsFile;
    private readonly int _uniqueCities;
    private readonly int _totalCities;
    private readonly Channel<Bucket> _channel;

    public VersionThree(string weatherStationsFile, string mesurementsFile, int uniqueCities, int totalCities)
    {
        _weatherStationsFile = weatherStationsFile;
        _mesurementsFile = mesurementsFile;
        _uniqueCities = uniqueCities;
        _totalCities = totalCities;

        _channel = Channel.CreateBounded<Bucket>(new BoundedChannelOptions(1000)
        {
            SingleWriter = false,
            SingleReader = true,
            AllowSynchronousContinuations = true
        });
    }

    public async Task SeedData()
    {
        try
        {
            var documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Dictionary<byte[], float> stations = GetUniqueStations(documentsDir);
            var stationsList = stations.ToList();

            // A shared array pool is used to reduce the number of allocations
            ArrayPool<byte> arrayPool = ArrayPool<byte>.Shared;
            var measurementsPath = Path.Combine(documentsDir, _mesurementsFile);

            // Divide _totalCities by 7 to get the workload
            int workload = _totalCities / 7;
            int remainder = _totalCities % 7;

            // Create 7 new threads, one of which will write the remaining workload
            List<Thread> threads = new List<Thread>();
            Thread unluckyThread = new Thread(() => WriteRandomStationsNew(stationsList, _channel, arrayPool, workload + remainder));
            threads.Add(unluckyThread);
            unluckyThread.Start();
            for (int i = 0; i < 6; i++)
            {
                // Call the WriteRandomStationsNew method with the channel and the array pool
                Thread thread = new Thread(() => WriteRandomStationsNew(stationsList, _channel, arrayPool, workload));
                threads.Add(thread);
                thread.Start();
            }

            var threadManager = Task.Run(() =>
            {
                foreach (var thread in threads)
                {
                    thread.Join();
                }
                _channel.Writer.Complete();
            });

            using (var fs = File.OpenWrite(measurementsPath))
            {
                while (await _channel.Reader.WaitToReadAsync())
                {
                    var bucket = await _channel.Reader.ReadAsync();
                    fs.Write(bucket.data.AsSpan(0, bucket.activeIdx));
                    arrayPool.Return(bucket.data);
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

    public Dictionary<byte[], float> GetUniqueStations(string documentsDir)
    {
        try
        {
            var stations = new Dictionary<byte[], float>();
            var filePath = Path.Combine(documentsDir, _weatherStationsFile);
            using var file = File.OpenRead(filePath);
            using var stream = new StreamReader(file);
            var lineNo = 0;
            while (!stream.EndOfStream && lineNo < _uniqueCities)
            {
                var line = stream.ReadLine();
                var parts = line!.Split(';').Select(x => Encoding.UTF8.GetBytes(x)).ToArray();
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

    private void WriteRandomStationsNew(List<KeyValuePair<byte[], float>> stations, Channel<Bucket> channel, ArrayPool<byte> pool, int workload)
    {
        try
        {
            var rand = new Random();
            var buffer = new byte[16];
            byte[] bucket = pool.Rent(8192);
            int bucketIdx = 0;
            for (int lineNo = 0; lineNo < workload; lineNo++)
            {
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
                        while (!channel.Writer.TryWrite(new Bucket(bucket, bucketIdx)))
                            _channel.Writer.WaitToWriteAsync().AsTask().Wait();
                        bucketIdx = 0;
                        bucket = pool.Rent(8192);
                    }
                    // add station
                    station.Key.AsSpan().CopyTo(bucket.AsSpan().Slice(bucketIdx));
                    bucketIdx += station.Key.Length;
                    // add ';'
                    bucket[bucketIdx] = (byte)';';
                    bucketIdx++;
                    // add temperature
                    buffer[0..bytesWritten].CopyTo(bucket.AsSpan().Slice(bucketIdx));
                    bucketIdx += bytesWritten;
                    // add '\n'
                    bucket[bucketIdx] = (byte)'\n';
                    bucketIdx++;
                }
            }

            // Write the remaining bucket to the stream
            if (bucketIdx > 0)
                while (!channel.Writer.TryWrite(new Bucket(bucket, bucketIdx)))
                    _channel.Writer.WaitToWriteAsync().AsTask().Wait();
            else pool.Return(bucket);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }
}
