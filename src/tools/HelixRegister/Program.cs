using System.Text.Json;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

// helix-register - the macOS answer to the Plug-in Registration tool (ADR-021).
//
//   helix-register push          upload or update the plug-in package
//   helix-register steps         apply every step and image in registration.json
//   helix-register all           push, then steps
//   helix-register trace on|off  set the environment's plug-in trace log level
//   helix-register smoke         create a case with no UI involved and read it back
//
// It never deletes anything. See B14.

var verb = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

var url = Environment.GetEnvironmentVariable("HELIX_DEV_URL");
if (string.IsNullOrWhiteSpace(url))
{
    Console.Error.WriteLine("HELIX_DEV_URL is not set. Run: export HELIX_DEV_URL=https://<yourorg>.crm11.dynamics.com");
    return 2;
}

// The AppId and RedirectUri below are the ones Microsoft publishes for development and
// prototyping. Phase 2 replaces them with a dedicated app registration and a client
// secret in Key Vault; recorded as a RAID assumption, not left as a surprise.
var connectionString =
    $"AuthType=OAuth;Url={url};" +
    "AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;" +
    "RedirectUri=http://localhost;" +
    "LoginPrompt=Auto;RequireNewInstance=True";

using var client = new ServiceClient(connectionString);
if (!client.IsReady)
{
    Console.Error.WriteLine($"Could not connect to {url}: {client.LastError}");
    return 3;
}

IOrganizationService service = client;
var who = (WhoAmIResponse)service.Execute(new WhoAmIRequest());
Console.WriteLine($"Connected to {url} as user {who.UserId}.");

var manifest = LoadManifest();

try
{
    switch (verb)
    {
        case "push": PushPackage(service, manifest); break;
        case "steps": RegisterSteps(service, manifest); break;
        case "all": PushPackage(service, manifest); RegisterSteps(service, manifest); break;
        case "trace": SetTrace(service, args.Length > 1 ? args[1] : "on"); break;
        case "smoke": Smoke(service, manifest); break;
        default:
            Console.Error.WriteLine($"Unknown verb '{verb}'. Use push | steps | all | trace | smoke.");
            return 1;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FAILED: {ex.Message}");
    Console.Error.WriteLine(ex.ToString());
    return 4;
}

return 0;

// ---------------------------------------------------------------- manifest

static Manifest LoadManifest()
{
    var path = Path.Combine(System.AppContext.BaseDirectory, "registration.json");
    var json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<Manifest>(json,
               new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
           ?? throw new InvalidOperationException($"Could not read {path}.");
}

// ---------------------------------------------------------------- package

static void PushPackage(IOrganizationService service, Manifest m)
{
    var nupkg = Path.GetFullPath(Path.Combine(RepoRoot(), m.Nupkg));
    if (!File.Exists(nupkg))
        throw new FileNotFoundException(
            $"Package not found at {nupkg}. Run 'dotnet pack -c Release' in the plug-in project first.", nupkg);

    var content = Convert.ToBase64String(File.ReadAllBytes(nupkg));
    var existing = FirstOrNull(service, "pluginpackage", "uniquename", m.PackageUniqueName,
                               new ColumnSet("pluginpackageid", "name", "version"));

    if (existing is null)
    {
        var row = new Entity("pluginpackage")
        {
            ["name"] = m.PackageUniqueName,
            ["uniquename"] = m.PackageUniqueName,
            ["version"] = m.PackageVersion,
            ["content"] = content
        };
        var request = new CreateRequest { Target = row };
        request["SolutionUniqueName"] = m.SolutionUniqueName;      // files it into Helix_Core as it is made
        var id = ((CreateResponse)service.Execute(request)).id;
        Console.WriteLine($"  created package {m.PackageUniqueName} {m.PackageVersion} -> {id}");
        Console.WriteLine($"  ONE-WAY DOOR PASSED: the name and version above are now permanent.");
    }
    else
    {
        // Deliberately sending ONLY content. Name and version cannot be changed once the
        // package exists, and attempting it returns an error.
        var row = new Entity("pluginpackage", existing.Id) { ["content"] = content };
        var request = new UpdateRequest { Target = row };
        request["SolutionUniqueName"] = m.SolutionUniqueName;
        service.Execute(request);
        Console.WriteLine($"  updated package {m.PackageUniqueName} -> {existing.Id}");
    }
}

// ---------------------------------------------------------------- steps

static void RegisterSteps(IOrganizationService service, Manifest m)
{
    foreach (var s in m.Steps)
    {
        var typeId = RequireId(service, "plugintype", "typename", s.TypeName,
            "Push the package first, and check that TypeName is the namespace-qualified class name.");
        var messageId = RequireId(service, "sdkmessage", "name", s.Message, "Unknown SDK message.");
        var filterId = MessageFilter(service, messageId, s.PrimaryEntity, s.Message);

        var step = new Entity("sdkmessageprocessingstep")
        {
            ["name"] = s.Name,
            ["description"] = s.Description,
            ["plugintypeid"] = new EntityReference("plugintype", typeId),
            ["sdkmessageid"] = new EntityReference("sdkmessage", messageId),
            ["sdkmessagefilterid"] = new EntityReference("sdkmessagefilter", filterId),
            ["stage"] = new OptionSetValue(s.Stage),
            ["mode"] = new OptionSetValue(s.Mode),
            ["rank"] = s.Rank,
            ["supporteddeployment"] = new OptionSetValue(0),   // 0 = server only
            ["asyncautodelete"] = false
        };
        if (!string.IsNullOrWhiteSpace(s.FilteringAttributes))
            step["filteringattributes"] = s.FilteringAttributes;

        var existing = FirstOrNull(service, "sdkmessageprocessingstep", "name", s.Name,
                                   new ColumnSet("sdkmessageprocessingstepid"));
        Guid stepId;

        if (existing is null)
        {
            var request = new CreateRequest { Target = step };
            request["SolutionUniqueName"] = m.SolutionUniqueName;
            stepId = ((CreateResponse)service.Execute(request)).id;
            Console.WriteLine($"  registered step  {s.Name}");
        }
        else
        {
            step.Id = existing.Id;
            var request = new UpdateRequest { Target = step };
            request["SolutionUniqueName"] = m.SolutionUniqueName;
            service.Execute(request);
            stepId = existing.Id;
            Console.WriteLine($"  updated step     {s.Name}");
        }

        foreach (var img in s.Images)
        {
            var image = new Entity("sdkmessageprocessingstepimage")
            {
                ["name"] = img.Name,
                ["entityalias"] = img.Alias,
                ["imagetype"] = new OptionSetValue(img.ImageType), // 0 pre, 1 post, 2 both
                ["messagepropertyname"] = "Target",
                ["attributes"] = img.Attributes,
                ["sdkmessageprocessingstepid"] = new EntityReference("sdkmessageprocessingstep", stepId)
            };

            var existingImage = FirstOrNull(service, "sdkmessageprocessingstepimage", "name", img.Name,
                                            new ColumnSet("sdkmessageprocessingstepimageid"));
            if (existingImage is null)
            {
                var request = new CreateRequest { Target = image };
                request["SolutionUniqueName"] = m.SolutionUniqueName;
                service.Execute(request);
                Console.WriteLine($"    + image        {img.Alias} ({img.Attributes})");
            }
            else
            {
                image.Id = existingImage.Id;
                var request = new UpdateRequest { Target = image };
                request["SolutionUniqueName"] = m.SolutionUniqueName;
                service.Execute(request);
                Console.WriteLine($"    ~ image        {img.Alias} ({img.Attributes})");
            }
        }
    }
}

static string RepoRoot()
{
    var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
    while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
        dir = dir.Parent;

    if (dir is null)
        throw new InvalidOperationException(
            $"No .git folder found above {System.AppContext.BaseDirectory}. " +
            "Run this from inside the repository.");

    return dir.FullName;
}

// ---------------------------------------------------------------- tracing

static void SetTrace(IOrganizationService service, string onOff)
{
    var level = onOff.ToLowerInvariant() switch
    {
        "on" or "all" => 2,   // write on completion or exception
        "exceptions" => 1,
        "off" => 0,
        _ => throw new ArgumentException("Use: trace on | exceptions | off")
    };

    var query = new QueryExpression("organization")
    {
        ColumnSet = new ColumnSet("organizationid", "plugintracelogsetting"),
        TopCount = 1
    };
    var org = service.RetrieveMultiple(query).Entities.Single();

    service.Update(new Entity("organization", org.Id)
    {
        ["plugintracelogsetting"] = new OptionSetValue(level)
    });
    Console.WriteLine($"  plug-in trace log setting = {level} ({onOff}).");
}

// ---------------------------------------------------------------- smoke test

static void Smoke(IOrganizationService service, Manifest m)
{
    var caseType = FirstOrNull(service, m.Smoke.CaseTypeTable, m.Smoke.CaseTypeNameColumn,
                               m.Smoke.CaseTypeName, new ColumnSet(false))
        ?? throw new InvalidOperationException($"No '{m.Smoke.CaseTypeName}' row in {m.Smoke.CaseTypeTable}.");

    var contact = FirstOrNull(service, m.Smoke.ContactTable, m.Smoke.ContactNameColumn,
                              m.Smoke.ContactName, new ColumnSet(false))
        ?? throw new InvalidOperationException($"No '{m.Smoke.ContactName}' row in {m.Smoke.ContactTable}.");

    var row = new Entity(m.Smoke.CaseTable)
    {
        [m.Smoke.SummaryColumn] = "Smoke test raised by helix-register. No app, no form, no business rule.",
        [m.Smoke.CaseTypeLookup] = new EntityReference(m.Smoke.CaseTypeTable, caseType.Id),
        [m.Smoke.ContactLookup] = new EntityReference(m.Smoke.ContactTable, contact.Id)
    };

    var id = service.Create(row);
    var created = service.Retrieve(m.Smoke.CaseTable, id,
        new ColumnSet(m.Smoke.ReferenceColumn, m.Smoke.PriorityColumn, m.Smoke.DivisionLookup));

    Console.WriteLine($"  created case {id}");
    Console.WriteLine($"    reference = {created.GetAttributeValue<string>(m.Smoke.ReferenceColumn) ?? "(null)"}");
    Console.WriteLine($"    priority  = {created.GetAttributeValue<OptionSetValue>(m.Smoke.PriorityColumn)?.Value.ToString() ?? "(null)"}");
    Console.WriteLine($"    division  = {created.GetAttributeValue<EntityReference>(m.Smoke.DivisionLookup)?.Name ?? "(null)"}");
}

// ---------------------------------------------------------------- helpers

static Entity? FirstOrNull(IOrganizationService service, string table, string column, string value, ColumnSet columns)
{
    var query = new QueryExpression(table) { ColumnSet = columns, TopCount = 1 };
    query.Criteria.AddCondition(column, ConditionOperator.Equal, value);
    return service.RetrieveMultiple(query).Entities.FirstOrDefault();
}

static Guid RequireId(IOrganizationService service, string table, string column, string value, string hint)
{
    var row = FirstOrNull(service, table, column, value, new ColumnSet(false));
    if (row is null)
        throw new InvalidOperationException($"No {table} where {column} = '{value}'. {hint}");
    return row.Id;
}

static Guid MessageFilter(IOrganizationService service, Guid messageId, string primaryEntity, string message)
{
    var query = new QueryExpression("sdkmessagefilter")
    {
        ColumnSet = new ColumnSet("sdkmessagefilterid"),
        TopCount = 1
    };
    query.Criteria.AddCondition("sdkmessageid", ConditionOperator.Equal, messageId);
    query.Criteria.AddCondition("primaryobjecttypecode", ConditionOperator.Equal, primaryEntity);
    query.Criteria.AddCondition("iscustomprocessingstepallowed", ConditionOperator.Equal, true);

    var row = service.RetrieveMultiple(query).Entities.FirstOrDefault();
    if (row is null)
        throw new InvalidOperationException(
            $"'{primaryEntity}' does not allow a custom step on '{message}'. Check the table and message names.");
    return row.Id;
}

// ---------------------------------------------------------------- manifest shape

sealed record Manifest(
    string SolutionUniqueName,
    string PackageUniqueName,
    string PackageVersion,
    string Nupkg,
    StepDef[] Steps,
    SmokeDef Smoke);

sealed record StepDef(
    string Name,
    string Description,
    string TypeName,
    string Message,
    string PrimaryEntity,
    int Stage,
    int Mode,
    int Rank,
    string? FilteringAttributes,
    ImageDef[] Images);

sealed record ImageDef(string Name, string Alias, int ImageType, string Attributes);

sealed record SmokeDef(
    string CaseTable, string SummaryColumn, string ReferenceColumn, string PriorityColumn,
    string CaseTypeLookup, string ContactLookup, string DivisionLookup,
    string CaseTypeTable, string CaseTypeNameColumn, string CaseTypeName,
    string ContactTable, string ContactNameColumn, string ContactName);