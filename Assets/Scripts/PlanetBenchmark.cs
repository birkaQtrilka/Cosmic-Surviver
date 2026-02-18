using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
public struct RunData
{
    public double time;
    public RunData(double time)
    {
        this.time = time;
    }
}

public class PlanetBenchmark : MonoBehaviour
{
    public Generator planetGenerator;
    public bool showDebug;
    public int iterations = 10;

    [ContextMenu("Benchmark Planet")]
    public void RunBenchmark()
    {
        if (planetGenerator == null)
        {
            UnityEngine.Debug.LogError("Planet generator reference missing!");
            return;
        }
        List<RunData> data = new List<RunData>();
        Stopwatch stopwatch = new Stopwatch();
        double totalTime = 0;
        if (showDebug) UnityEngine.Debug.Log($"Starting benchmark ({iterations} runs)...");

        for (int i = 0; i < iterations; i++)
        {
            stopwatch.Restart();

            planetGenerator.GeneratePlanet();

            stopwatch.Stop();

            double ms = stopwatch.Elapsed.TotalMilliseconds;
            totalTime += ms;

            if (showDebug) UnityEngine.Debug.Log($"Run {i + 1}: {ms:F2} ms");

            data.Add(new RunData { time = ms });
        }

        double average = totalTime / iterations;

        if (showDebug) UnityEngine.Debug.Log($"-----------------------------------");
        if (showDebug) UnityEngine.Debug.Log($"Average Time: {average:F2} ms");
        if (showDebug) UnityEngine.Debug.Log($"Total Time: {totalTime:F2} ms");
        if (showDebug) UnityEngine.Debug.Log($"-----------------------------------");
        WriteOnCsv(data, average);
    }
    [ContextMenu("CSV Test")]
    public void CSVTest()
    {
        var mockList = new List<RunData>
        {
            new RunData(12.34),
            new RunData(11.87),
            new RunData(13.02),
            new RunData(12.65),
            new RunData(11.95),
            new RunData(12.10),
            new RunData(12.88),
            new RunData(11.76),
            new RunData(12.43),
            new RunData(12.01)
        };
        double total = mockList.Sum(d => d.time);
        WriteOnCsv(mockList, total);
    }

    public void WriteOnCsv(List<RunData> dataList, double totalTime)
    {
        char s = ';';
        //string filePath = Path.Combine(Application.dataPath, "BoidsData.csv");
        string filePath = Path.Combine(Application.persistentDataPath, "BoidsData.csv");
        UnityEngine.Debug.Log("Writing to: " + filePath);
        using var writer = new StreamWriter(filePath, true);
        // Write header
        var culture = new CultureInfo("de-DE");
        // Write each record
        foreach (var data in dataList)
        {
            writer.Write($"{data.time.ToString(culture)}{s}");
        }
        writer.WriteLine($"{s}{totalTime.ToString(culture)}");

    }
}
