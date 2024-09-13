using System.Globalization;
using System.IO.Ports;
using CsvHelper;

namespace CamCapture;

class Program
{
    public SerialPort ArduinoPort { get; set; } = new SerialPort("COM10", 9600);
    public List<Measurement> Measurements { get; set; } = [];

    static void Main(string[] args)
    {
    }

    public void Measure()
    {
        const int steps = 720;
        Console.WriteLine("Opening port...");
        try
        {
            ArduinoPort.Open();
            Console.WriteLine("Port opened successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open port: {ex.Message}");
            return;
        }
        for (int i = 0; i < steps; i++)
        {
            Console.WriteLine($"Step {i+1}/{steps}");
            // Send command to step
            ArduinoPort.WriteLine("STEP");
            var stepResult = ArduinoPort.ReadLine();
            if (!stepResult.StartsWith("ACK"))
                throw new InvalidOperationException("Failed to send step command");

            // Send command to measure
            ArduinoPort.WriteLine("MEASURE");
            var measureResult = ArduinoPort.ReadLine();
            if(string.IsNullOrEmpty(measureResult))
                throw new InvalidOperationException($"Failed to receive measurement result, got '{measureResult}'");
            var parsedOk = decimal.TryParse(measureResult, out var value);
            if (!parsedOk)
                throw new InvalidOperationException($"Failed to parse measurement result '{measureResult}' as decimal");

            Measurements.Add(new Measurement(){Step = i/(steps/360m), Value = value  });
            Console.WriteLine($"Value: {value:F3}mm");
        }
        ArduinoPort.Close();
            
        try
        {
            using (var writer = new StreamWriter("c:\\temp\\output.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(Measurements);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to CSV: {ex.Message}");
        }

    }
}

public class Measurement
{
    public decimal Step { get; set; }
    public decimal Value { get; set; }
}