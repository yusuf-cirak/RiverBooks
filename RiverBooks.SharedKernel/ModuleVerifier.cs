using System.Reflection;
using System.Text;
using System.Text.Json;

namespace RiverBooks.SharedKernel;
public static class ModuleVerifier
{
  private const string ContractsTemplate = "RiverBooks.{0}.Contracts";
  public static void RunForHost()
  {
    var microServiceModules = GetModules()
    .Where(mi => mi.IsMicroservice)
    .ToList();


    var microServiceContracts = microServiceModules
      .Select(msm => string.Format(ContractsTemplate, msm.Name))
      .ToList();


    if (!microServiceModules.Any())
    {
      Console.WriteLine("There are no microservices in your project.");
      return;
    }


    microServiceModules.ForEach(mi => Console.WriteLine($"'{mi.Name}' is a microservice."));


    Console.WriteLine("Starting to check for dependencies...");


    VerifyModuleDependencies(microServiceContracts);

    Console.WriteLine("Successfully verified all module dependencies!");
  }


  private static List<ModuleInfo> GetModules()
  {
    const string FileName = "services.json";

    string solutionDirectory = AppDomain.CurrentDomain.BaseDirectory;
    string filePath = Path.Combine(solutionDirectory, "..", "..", "..", "..", FileName);

    if (!File.Exists(filePath))
    {
      throw new FileNotFoundException($"The file {FileName} could not be found at {filePath}");
    }

    var bytes = File.ReadAllBytes(filePath);


    return JsonSerializer.Deserialize<List<ModuleInfo>>(bytes) ?? [];
  }



  private static void VerifyModuleDependencies(List<string> microServiceContracts)
  {
    var dependingAssemblies = new List<Assembly>();

    // Get all assemblies in the current AppDomain
    var assemblies = AppDomain
      .CurrentDomain
      .GetAssemblies()
      .Where(assembly =>
      {
        var assemblyName = assembly.GetName().Name!;

        return assemblyName.StartsWith("RiverBooks") && !microServiceContracts.Exists(msc => msc.Contains(assemblyName));
      });



    var errorMessages = new StringBuilder();

    // Iterate through all assemblies to find dependencies
    foreach (var assembly in assemblies)
    {
      // Get the referenced assemblies
      var referencedAssemblies = assembly
        .GetReferencedAssemblies()
        .Where(refAssembly =>
        {
          var refAssemblyName = refAssembly.Name!;

          return refAssemblyName.StartsWith("RiverBooks") && microServiceContracts.Exists(msc => msc.Equals(refAssemblyName));
        })
        .ToList();


      microServiceContracts.ForEach(mscAssembly =>
      {
        var hasDependency = referencedAssemblies.Exists(ra => ra.Name == mscAssembly);

        if (hasDependency)
        {
          errorMessages.AppendLine($"{assembly.GetName().Name} has dependency to a microservice contract project: {mscAssembly}");
        }
      });
    }

    if (errorMessages.Length > 0)
    {
      throw new InvalidOperationException(errorMessages.ToString());
    }
  }

}


public class ModuleInfo
{
  public string Name { get; set; } = string.Empty;
  public bool IsMicroservice { get; set; }

}
