using Newtonsoft.Json;

var sample = new { Name = "Security Coursework", Variant = 25, Status = "Vulnerable Demo Dependency" };
var json = JsonConvert.SerializeObject(sample);
Console.WriteLine(json);
