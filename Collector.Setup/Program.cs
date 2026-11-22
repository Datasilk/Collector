using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

// Collector.Setup - interactive deploy-time configuration.
// Replaces setup.bat / setup-docker.bat / update-config.ps1.
// Runs from the deployment root next to the published app folders and
// the Deploy\ folder copied by deploy.bat.
//
// Flow: host question, then per-project blocks (Collector > Charlotte >
// Router). Saying "no" to a project skips every question for it.

// tee all console output into logs\{timestamp}.log next to setup.exe -
// the updater launches `setup.exe --skip` with no visible console, and
// this is the same always-on log format the updater itself uses.
var conOut = Console.Out;
var conErr = Console.Error;
string? setupLogPath = null;
try
{
    var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
    Directory.CreateDirectory(logDir);
    setupLogPath = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd-HH-mm}.log");
    Console.SetOut(new TeeWriter(conOut, setupLogPath));
    Console.SetError(new TeeWriter(conErr, setupLogPath));
    Console.WriteLine($"[{DateTime.Now:u}] started setup.exe ({string.Join(' ', args)})");
}
catch { }
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    try
    {
        File.AppendAllText(
            setupLogPath ?? Path.Combine(Path.GetTempPath(), "collector-setup-crash.log"),
            $"[{DateTime.Now:u}] FATAL: {e.ExceptionObject}{Environment.NewLine}");
    }
    catch { }
};

var root = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
var statePath = Path.Combine(root, "setup.json");
// --skip: non-interactive run (launched by the Collector Updater service
// after a new setup.exe is published). All answers come from setup.json.
var skip = args.Contains("--skip", StringComparer.OrdinalIgnoreCase);

Console.WriteLine();
Console.WriteLine("============================================");
Console.WriteLine("  Collector & Charlotte Setup");
Console.WriteLine("============================================");
if (skip) Console.WriteLine("  --skip: applying setup.json non-interactively");
Console.WriteLine();

// Stop the CollectorUpdater service first - it can't fire on files we
// write mid-setup, and its bin\updater.exe handle releases while we ask
// questions, so the bin refresh later won't hit a lock. Silent when the
// service isn't registered or is already stopped.
if (OperatingSystem.IsWindows() &&
    new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
{
    try
    {
        using var p = Process.Start(new ProcessStartInfo("sc", "stop CollectorUpdater")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });
        p?.WaitForExit(10_000);
    }
    catch { }
}

// --- load saved choices ---
var saved = LoadState(statePath);
string? S(string k) => saved.TryGetValue(k, out var v) && v.Length > 0 ? v : null;

// Backfill passwords into the dictionary from the live configs when
// setup.json lacks them - --skip runs need these keys to rebuild the
// conn string / router config without prompting.
if (S("PgPassword") is null)
{
    var cfgDir = Path.Combine(root, "Collector.Web.Server");
    if (Directory.Exists(cfgDir))
        foreach (var f in Directory.EnumerateFiles(cfgDir, "appsettings*.json"))
        {
            var cs = ConnParts(LoadJsonObj(f)?["ConnectionStrings"]?["Database"]?.GetValue<string>());
            if (cs.TryGetValue("Password", out var pw) && pw.Length > 0)
            {
                saved["PgPassword"] = pw;
                break;
            }
        }
}
var routerCfg = LoadJsonObj(Path.Combine(root, "CharlottesWeb", "config.json"));
if (routerCfg?["Admin"]?["Username"]?.GetValue<string>() is string au && au.Length > 0)
    saved.TryAdd("AdminUser", au);
if (routerCfg?["Admin"]?["Pass"]?.GetValue<string>() is string ap && ap.Length > 0)
    saved.TryAdd("AdminPass", ap);

var registered = new List<string>();      // windows services registered

// --updater: launched by the CollectorUpdater service after a deploy
// finishes (it bounces containers, then spawns us). Just swap the
// freshly published updater.exe into bin\ and restart the service -
// no questions, no project config.
if (args.Contains("--updater", StringComparer.OrdinalIgnoreCase))
{
    EnsureUpdaterService();
    return;
}

// --- host choice: first question ---
var hostHint = (S("Host") ?? "Windows").Equals("Docker", StringComparison.OrdinalIgnoreCase) ? "D" : "W";
var hostInput = Prompt("Set up for (W)indows services or (D)ocker containers?", hostHint);
var docker = hostInput.StartsWith('D') || hostInput.StartsWith('d');
saved["Host"] = docker ? "Docker" : "Windows";
Console.WriteLine();

if (docker && !ToolExists("docker"))
    Fail("docker not found on PATH - install Docker Desktop first.");

// existing containers? 'collector' and 'charlottesweb' have fixed
// container_name; charlotte scales to collector-charlotte-1..N.
var dockerContainerNames = docker
    ? ExecOut("docker", "ps -a --format \"{{.Names}}\"") : "";
var collectorExists = ContainerNamed("collector");
var routerExists = ContainerNamed("charlottesweb");
var charlotteContainerNames = Regex.Matches(dockerContainerNames,
        @"^collector-charlotte-\d+$", RegexOptions.Multiline)
    .Select(m => m.Value).ToList();
var charlotteReplicaCount = charlotteContainerNames.Count;
var removeList = new List<string>();   // containers the user opted out of
bool ContainerNamed(string name) => Regex.IsMatch(dockerContainerNames,
    $"(?m)^{Regex.Escape(name)}$");

var dockerDir = Path.Combine(root, "Deploy", "docker");
var webServerDir = Path.Combine(root, "Collector.Web.Server");
var services = new List<string>();        // docker services to compose up
string? scaleArg = null;
var doBrowsers = false;
var charlotteCount = "1";
string env = "Production";
string? contentPath = null;
var appSettingsPath = Path.Combine(webServerDir, "appsettings.json");

// ============================================================
//  Collector
// ============================================================
var setupCollector = docker
    ? PromptYN("Run Collector as a Docker container?", collectorExists || S("RunCollector") == "Y")
    : PromptYN("Register Collector as a Windows Service?", ServiceExists("Collector") || S("RegCollector") == "Y");
if (docker && collectorExists && !setupCollector) removeList.Add("collector");
saved[docker ? "RunCollector" : "RegCollector"] = setupCollector ? "Y" : "N";

if (setupCollector)
{
    // environment decides which appsettings.*.json wins & gets written
    var envHint = (S("Environment") ?? "Production") == "Development" ? "D" : "P";
    var envInput = Prompt("(D)evelopment or (P)roduction environment?", envHint);
    env = envInput.StartsWith('D') || envInput.StartsWith('d') ? "Development" : "Production";
    saved["Environment"] = env;

    // ports - skip entirely when setup.json already has all four
    string feHttp = S("FeHttp") ?? "7784", feHttps = S("FeHttps") ?? "7783",
           beHttp = S("BeHttp") ?? "7780", beHttps = S("BeHttps") ?? "7781";
    if (S("FeHttp") is not null && S("FeHttps") is not null
        && S("BeHttp") is not null && S("BeHttps") is not null)
    {
        Console.WriteLine($"  Ports kept from setup.json (fe {feHttp}/{feHttps} be {beHttp}/{beHttps})");
    }
    else
    {
        feHttp = Prompt("React front end HTTP port", feHttp);
        feHttps = Prompt("React front end HTTPS port", feHttps);
        beHttp = Prompt("Server backend HTTP port", beHttp);
        beHttps = Prompt("Server backend HTTPS port", beHttps);
    }
    saved["FeHttp"] = feHttp; saved["FeHttps"] = feHttps;
    saved["BeHttp"] = beHttp; saved["BeHttps"] = beHttps;

    var envSettingsPath = Path.Combine(webServerDir, $"appsettings.{env}.json");
    // effective config = appsettings.json with appsettings.{env}.json
    // merged on top (env file can mask base values, e.g. a stale
    // 127.0.0.1 conn string).
    var existing = LoadJsonObj(appSettingsPath) ?? new JsonObject();
    var overlay = LoadJsonObj(envSettingsPath);
    if (overlay is not null) MergeInto(existing, overlay);

    if (docker)
    {
        // Ollama URL - localhost translates to the docker-host alias
        var curOllama = existing?["Ollama"]?["Url"]?.GetValue<string>();
        var ollamaUrl = (curOllama is not null && curOllama == S("OllamaUrl"))
            ? curOllama
            : TranslateLocalhost(Prompt("Ollama server URL", S("OllamaUrl") ?? "http://localhost:11434"));
        saved["OllamaUrl"] = ollamaUrl;

        // PostgreSQL - skip when the live connection string matches the
        // saved host/database/username (password stays untouched).
        var cs = ConnParts(existing?["ConnectionStrings"]?["Database"]?.GetValue<string>());
        string? pgHost = null, pgDb = null, pgUser = null, pgPass = null;
        var savedHost = S("PgHost");
        var savedDb = S("PgDatabase");
        var savedUser = S("PgUser");
        if (savedHost is not null
            && cs.TryGetValue("Host", out var h) && h == savedHost
            && cs.TryGetValue("Database", out var d) && d == savedDb
            && cs.TryGetValue("Username", out var u) && u == savedUser)
        {
            Console.WriteLine("  PostgreSQL connection already configured - keeping existing settings.");
            Console.WriteLine("  (edit setup.json or the appsettings file to change it)");
            // backfill the password so --skip runs can rebuild the string
            if (cs.TryGetValue("Password", out var csPw) && S("PgPassword") is null)
                saved["PgPassword"] = csPw;
        }
        else
        {
            pgHost = TranslateLocalhost(Prompt("PostgreSQL host", S("PgHost") ?? "localhost"));
            pgDb = Prompt("PostgreSQL database", S("PgDatabase") ?? "Collector");
            pgUser = Prompt("PostgreSQL username", S("PgUser") ?? "postgres");
            // default = saved password, else the password already in the
            // live connection string - Enter keeps it, typing replaces it.
            pgPass = PromptPassword("PostgreSQL password",
                S("PgPassword") ?? (cs.TryGetValue("Password", out var oldPw) ? oldPw : null));
            if (pgPass.Length == 0) Fail("a PostgreSQL password is required.");
            saved["PgHost"] = pgHost; saved["PgDatabase"] = pgDb;
            saved["PgUser"] = pgUser; saved["PgPassword"] = pgPass;
        }

        // content folder - skip when docker\.env CONTENT_PATH already
        // matches the saved answer, or Storage.Articles already resolves
        // to the same root / the docker mount point.
        var envContent = ReadEnvFile(Path.Combine(dockerDir, ".env"))
            .GetValueOrDefault("CONTENT_PATH")?.TrimEnd('/', '\\');
        var liveArticles = existing?["Storage"]?["Articles"]?.GetValue<string>();
        var liveRoot = liveArticles is null ? null
            : Regex.Replace(liveArticles, @"[\\/]articles[\\/]?$", "",
                RegexOptions.IgnoreCase).TrimEnd('/', '\\');
        var savedContent = S("ContentPath")?.TrimEnd('/', '\\');
        var dockerManaged = liveArticles?.StartsWith("/collector-content") == true;
        if (savedContent is not null && (dockerManaged
            || (envContent is not null && savedContent.Equals(envContent, StringComparison.OrdinalIgnoreCase))
            || (liveRoot is not null && savedContent.Equals(liveRoot, StringComparison.OrdinalIgnoreCase))))
        {
            contentPath = savedContent;
            Console.WriteLine("  Content folder already configured - keeping existing path.");
        }
        else
        {
            contentPath = Prompt("Collector content data folder", S("ContentPath") ?? envContent);
            if (contentPath.Length == 0) Fail("a content folder is required for the collector container.");
            contentPath = contentPath.TrimEnd('/', '\\');
            saved["ContentPath"] = contentPath;
        }
        Console.WriteLine();

        ApplyWebConfig(envSettingsPath, feHttp, feHttps, beHttp, beHttps,
            contentPrefix: "/collector-content",
            ollamaUrl: ollamaUrl, pgHost: pgHost, pgDb: pgDb,
            pgUser: pgUser, pgPass: pgPass);
        services.Add("collector");
    }
    else
    {
        // Windows - content prompt is optional; blank keeps existing paths
        var c = Prompt("Collector content data folder", S("ContentPath"));
        if (c.Length > 0)
        {
            contentPath = c.TrimEnd('/', '\\');
            saved["ContentPath"] = contentPath;
            if (!Directory.Exists(contentPath))
            {
                Console.WriteLine($"  Creating {contentPath} ...");
                Directory.CreateDirectory(contentPath);
            }
        }
        ApplyWebConfig(envSettingsPath, feHttp, feHttps, beHttp, beHttps,
            contentPrefix: contentPath);
        RegisterService("Collector", "Collector",
            Path.Combine(webServerDir, "Collector.Web.Server.exe"));
    }
    Console.WriteLine();
}

// ============================================================
//  Charlotte
// ============================================================
var setupCharlotte = docker
    ? PromptYN("Run Charlotte as a Docker container?", charlotteReplicaCount > 0 || S("RunCharlotte") == "Y")
    : PromptYN("Register Charlotte as a Windows Service?", ServiceExists("Charlotte") || S("RegCharlotte") == "Y");
if (docker && charlotteReplicaCount > 0 && !setupCharlotte)
    removeList.AddRange(charlotteContainerNames);
saved[docker ? "RunCharlotte" : "RegCharlotte"] = setupCharlotte ? "Y" : "N";

if (setupCharlotte)
{
    if (docker)
    {
        if (!File.Exists(Path.Combine(root, "Charlotte", "Charlotte.dll")))
            Console.WriteLine("  ERROR: Charlotte\\Charlotte.dll not found - run deploy.bat first.");
        else
        {
            charlotteCount = Prompt("Number of Charlotte instances to run",
                charlotteReplicaCount > 0 ? charlotteReplicaCount.ToString() : S("CharlotteCount") ?? "1");
            if (!int.TryParse(charlotteCount, out var n) || n < 1) charlotteCount = "1";
            saved["CharlotteCount"] = charlotteCount;
            services.Add("charlotte");
            scaleArg = $"--scale charlotte={charlotteCount}";
            doBrowsers = true;
        }
    }
    else
    {
        RegisterService("Charlotte", "Charlotte",
            Path.Combine(root, "Charlotte", "Charlotte.exe"));
    }
}
Console.WriteLine();

// ============================================================
//  Charlotte's Web Router
// ============================================================
var setupRouter = docker
    ? PromptYN("Run Charlotte's Web Router as a Docker container?", routerExists || S("RunRouter") == "Y")
    : PromptYN("Set up Charlotte's Web Router?", ServiceExists("CharlottesWeb") || S("SetupRouter") == "Y");
if (docker && routerExists && !setupRouter) removeList.Add("charlottesweb");
saved[docker ? "RunRouter" : "SetupRouter"] = setupRouter ? "Y" : "N";

if (setupRouter)
{
    var routerDir = Path.Combine(root, "CharlottesWeb");
    if (!Directory.Exists(routerDir))
    {
        Console.WriteLine($"  ERROR: {routerDir} not found - run deploy.bat first.");
    }
    else
    {
        // keep existing admin credentials when the router was configured before
        string? adminUser = null, adminPass = null;
        var existingRouter = LoadJsonObj(Path.Combine(routerDir, "config.json"));
        if (existingRouter?["Admin"]?["Username"] is not null
            && existingRouter["Admin"]?["Pass"] is not null)
        {
            Console.WriteLine("  Router admin credentials kept from existing config.json.");
            // backfill so --skip runs can pass them to WriteRouterConfig
            if (existingRouter["Admin"]?["Username"]?.GetValue<string>() is string existUser)
                saved.TryAdd("AdminUser", existUser);
            if (existingRouter["Admin"]?["Pass"]?.GetValue<string>() is string existPass)
                saved.TryAdd("AdminPass", existPass);
        }
        else
        {
            adminUser = Prompt("Admin account", S("AdminUser") ?? "admin");
            adminPass = PromptPassword("Admin password", S("AdminPass"));
            if (adminPass.Length == 0) Fail("an admin password is required for the router dashboard.");
            saved["AdminUser"] = adminUser; saved["AdminPass"] = adminPass;
        }

        if (docker)
        {
            // instance URLs are generated from the replica count - reuse
            // the charlotte count or ask when charlotte isn't running.
            if (!services.Contains("charlotte"))
            {
                charlotteCount = Prompt("Number of Charlotte instances the router manages",
                    S("CharlotteCount") ?? "1");
                if (!int.TryParse(charlotteCount, out var n) || n < 1) charlotteCount = "1";
                saved["CharlotteCount"] = charlotteCount;
            }
            var count = int.Parse(charlotteCount);
            WriteRouterConfig(routerDir, adminUser, adminPass,
                Enumerable.Range(1, count)
                    .Select(i => $"http://collector-charlotte-{i}:7077/Browser").ToList());
            foreach (var v in new[] { "config.docker.json", "config.docker.prod.json", "config.prod.json" })
                File.Copy(Path.Combine(routerDir, "config.json"), Path.Combine(routerDir, v), true);
            services.Add("charlottesweb");
        }
        else
        {
            var urls = new List<string>();
            if (skip)
            {
                // --skip: replay the saved instance list (no prompts)
                urls.AddRange((S("RouterInstances") ?? "")
                    .Split(';', StringSplitOptions.RemoveEmptyEntries));
            }
            else
            {
                while (true)
                {
                    var url = urls.Count == 0
                        ? Prompt("Charlotte instance URL", "http://localhost:7077")
                        : PromptYN("Set up another Charlotte instance?")
                            ? Prompt("Charlotte instance URL") : null;
                    if (url is null) break;
                    if (url.Length == 0) { Console.WriteLine("  No URL entered - skipping."); continue; }
                    urls.Add(url.TrimEnd('/'));
                }
                saved["RouterInstances"] = string.Join(';', urls);
            }
            if (urls.Count > 0)
            {
                WriteRouterConfig(routerDir, adminUser, adminPass,
                    urls.Select(u => u.Contains("/Browser") ? u : u + "/Browser").ToList());
                File.Copy(Path.Combine(routerDir, "config.json"),
                          Path.Combine(routerDir, "config.prod.json"), true);
            }
            if (skip ? ServiceExists("CharlottesWeb")
                     : PromptYN("Register Charlotte's Web Router as a Windows Service?"))
                RegisterService("CharlottesWeb", "Charlotte's Web Router",
                    Path.Combine(routerDir, "Charlotte.Router.exe"));
        }
    }
}
Console.WriteLine();

// ============================================================
//  Do the work
// ============================================================
SaveState(statePath, saved);

// watches setup.exe and runs 'setup.exe --skip' after future deploys
EnsureUpdaterService();

if (docker)
{
    // user answered N to a service that has a container - remove it
    if (removeList.Count > 0)
    {
        foreach (var cn in removeList)
            Console.WriteLine($"  Removing container {cn} ...");
        Exec("docker", $"rm -f {string.Join(' ', removeList)}");
    }

    if (contentPath is not null && !Directory.Exists(contentPath))
    {
        Console.WriteLine($"  Creating {contentPath} ...");
        Directory.CreateDirectory(contentPath);
    }

    // HTTPS dev cert: Kestrel binds https://0.0.0.0 inside the collector
    // container and loads /https/aspnetapp.pfx from the mounted dir.
    var certPassword = S("CertPassword");
    if (services.Contains("collector"))
    {
        var certDir = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile), ".aspnet", "https");
        var pfxPath = Path.Combine(certDir, "aspnetapp.pfx");
        if (!File.Exists(pfxPath))
        {
            certPassword ??= Guid.NewGuid().ToString("N");
            Console.WriteLine($"  Generating self-signed HTTPS certificate in {certDir} ...");
            try
            {
                Directory.CreateDirectory(certDir);
                using var rsa = RSA.Create(2048);
                var req = new CertificateRequest("CN=localhost", rsa,
                    HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                using var cert = req.CreateSelfSigned(
                    DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(5));
                File.WriteAllBytes(pfxPath, cert.Export(X509ContentType.Pfx, certPassword));
                Console.WriteLine($"  Certificate written to {pfxPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  WARNING: certificate generation failed - {ex.Message}");
            }
        }
        else if (certPassword is null)
        {
            Console.WriteLine($"  Found existing {pfxPath} - assuming saved password.");
            Console.WriteLine("  If it has a different password, set CertPassword in setup.json.");
        }
        if (certPassword is not null) saved["CertPassword"] = certPassword;
    }

    // docker/.env - collector keys fall back to saved/defaults when the
    // collector section was skipped so compose interpolation stays valid.
    var envFile = ReadEnvFile(Path.Combine(dockerDir, ".env"));
    Directory.CreateDirectory(dockerDir);
    File.WriteAllText(Path.Combine(dockerDir, ".env"),
        $"CONTENT_PATH={contentPath ?? envFile.GetValueOrDefault("CONTENT_PATH") ?? @"C:\temp\collector-content"}\n" +
        $"PORT_FE_HTTP={S("FeHttp") ?? "7784"}\nPORT_FE_HTTPS={S("FeHttps") ?? "7783"}\n" +
        $"PORT_BE_HTTP={S("BeHttp") ?? "7780"}\nPORT_BE_HTTPS={S("BeHttps") ?? "7781"}\n" +
        $"CERT_PASSWORD={certPassword ?? ""}\n" +
        $"APP_ENVIRONMENT={env}\n");
    Console.WriteLine($"  Wrote {dockerDir}\\.env");
    SaveState(statePath, saved);

    // shared network
    if (Exec("docker", "network inspect collector-net", quiet: true) != 0)
    {
        Exec("docker", "network create collector-net", quiet: true);
        Console.WriteLine("  Created docker network collector-net");
    }

    // one merged compose command - all containers belong to the single
    // 'collector' project; separate per-file 'up' reports orphans.
    if (services.Count > 0)
    {
        var files = string.Join(" ", new[] { "collector.yml", "charlotte.yml", "charlotte-router.yml" }
            .Select(f => $"-f \"{Path.Combine(dockerDir, f)}\""));
        var envArg = $"--env-file \"{Path.Combine(dockerDir, ".env")}\"";
        // never recreate containers that already exist: app binaries are
        // bind-mounted so 'restart' is all a redeploy needs. 'up -d' is
        // reserved for containers that are actually missing (or a
        // charlotte replica-count change, which only it can reconcile).
        var missing = new List<string>();
        foreach (var s in services)
        {
            var exists = s switch
            {
                "charlotte" => int.TryParse(charlotteCount, out var want)
                               && charlotteContainerNames.Count == want && want > 0,
                _ => ContainerNamed(s)
            };
            if (!exists) missing.Add(s);
        }
        var rc = 0;
        if (missing.Count > 0)
            rc = Exec("docker",
                $"compose {files} {envArg} up -d {scaleArg} {string.Join(' ', missing)}");
        if (rc == 0)
        {
            if (missing.Count == 0)
                Console.WriteLine("  Containers already exist - restarting only.");
            Exec("docker",
                $"compose {files} {envArg} restart {string.Join(' ', services)}", quiet: true);
        }
        if (rc != 0) Console.WriteLine("  Failed to start containers.");
        else
        {
            Console.WriteLine($"  Containers running: {string.Join(", ", services)}");
            if (doBrowsers)
            {
                var sh = Path.Combine(root, "Charlotte", "install-browsers.sh");
                File.Copy(Path.Combine(dockerDir, "install-browsers.sh"), sh, true);
                // restart (not recreate) keeps the container FS, so the
                // deps sentinel inside install-browsers.sh survives -
                // browsers + apt deps only reinstall when truly needed.
                for (var i = 1; i <= int.Parse(charlotteCount); i++)
                {
                    var cn = $"collector-charlotte-{i}";
                    Console.WriteLine($"  Installing Playwright browsers in {cn} - first run takes a few minutes...");
                    if (Exec("docker", $"exec {cn} bash /app/install-browsers.sh") != 0)
                        Console.WriteLine($"  WARNING: browser install failed in {cn}.");
                }
            }
        }
    }
    else Console.WriteLine("  No containers selected to run.");

    Console.WriteLine();
    Console.WriteLine("  Manage containers with:");
    Console.WriteLine($"  docker compose -f {dockerDir}\\collector.yml -f {dockerDir}\\charlotte.yml -f {dockerDir}\\charlotte-router.yml logs -f / stop / down");
}
else
{
    // record services for Deploy\Start|Stop|UnregisterServices.bat
    File.WriteAllText(Path.Combine(root, "config.json"),
        "{\n  \"Services\": [ " + string.Join(", ", registered.Select(s => $"\"{s}\"")) + " ]\n}\n");
    var startBat = Path.Combine(root, "Deploy", "StartServices.bat");
    if (File.Exists(startBat)) Exec("cmd", $"/c \"{startBat}\"");
}

Console.WriteLine();
Console.WriteLine("Done.");
return;

// ============================================================
//  Windows service registration
// ============================================================
void RegisterService(string name, string displayName, string exePath)
{
    if (ServiceExists(name))
    {
        registered.Add(name);
        Console.WriteLine($"  {name} service already registered.");
        return;
    }
    if (!IsAdmin())
        Fail("Registering services requires administrator privileges - run setup.exe as Administrator.");
    if (!File.Exists(exePath))
    {
        Console.WriteLine($"  ERROR: {exePath} not found.");
        return;
    }
    if (Exec("sc", $"create {name} binPath= \"{exePath}\" DisplayName= \"{displayName}\" start= auto") == 0)
        Console.WriteLine($"  {name} service registered.");
    else
        Console.WriteLine($"  Failed to register {name} service.");
    if (ServiceExists(name)) registered.Add(name);
}

bool IsAdmin() => OperatingSystem.IsWindows() &&
    new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

bool ServiceExists(string name) => Exec("sc", $"query {name}", quiet: true) == 0;

// registers + starts the Collector Updater service (setup.exe watcher).
// The service runs a COPY at Collector.Updater\bin\updater.exe so the
// publish output (Collector.Updater\updater.exe) is never locked when a
// new deploy overwrites it. Flow: stop service (if exists) -> wait 5s
// for the exe to release -> refresh bin copy -> register/start.
// best-effort: docker-mode runs are often unelevated, so warn instead of fail.
void EnsureUpdaterService()
{
    var srcExe = Path.Combine(root, "Collector.Updater", "updater.exe");
    var binExe = Path.Combine(root, "Collector.Updater", "bin", "updater.exe");
    if (!File.Exists(srcExe))
    {
        Console.WriteLine("  Collector.Updater\\updater.exe not found - skipping updater service.");
        return;
    }
    // clear stale markers before (re)starting the service - a leftover
    // _deploying would make the updater stop everything again, and a
    // leftover _deployed would retrigger --skip on every service start.
    foreach (var m in new[] { "_deploying", "_deployed" })
    {
        var marker = Path.Combine(root, "Collector.Updater", "bin", m);
        try { if (File.Exists(marker)) File.Delete(marker); } catch { }
    }

    var admin = IsAdmin();
    var hadService = ServiceExists("CollectorUpdater");
    if (hadService)
    {
        if (admin)
        {
            Console.WriteLine("  Stopping Collector Updater service ...");
            Exec("sc", "stop CollectorUpdater", quiet: true);
            // give the running exe time to fully release its file handle
            // before we overwrite bin\updater.exe.
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
        else
        {
            Console.WriteLine("  Updater service is running but we're not elevated -");
            Console.WriteLine("  the bin copy may fail while its exe is locked.");
        }
    }

    Directory.CreateDirectory(Path.GetDirectoryName(binExe)!);
    try
    {
        File.Copy(srcExe, binExe, true);
        Console.WriteLine($"  Updated {binExe}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  WARNING: could not update {binExe} - {ex.Message}");
    }

    if (!admin)
    {
        Console.WriteLine("  Not elevated - run setup.exe as Administrator once to register");
        Console.WriteLine("  the Collector Updater service (it re-runs setup.exe --skip on deploy).");
        return;
    }

    // services run in session 0 where per-user drive mappings (M: etc.)
    // don't exist - register the UNC path so SCM can find the exe.
    var svcExe = ToServicePath(binExe);
    if (svcExe != binExe)
        Console.WriteLine($"  Registering service with UNC path: {svcExe}");

    // resolve the real service name: 1078 ("name already in use") also
    // fires on a DISPLAY-name collision, so an older install may exist
    // under a different service name than CollectorUpdater.
    var svcName = hadService ? "CollectorUpdater"
        : ExecOut("powershell",
            "-NoProfile -Command \"(Get-Service -DisplayName 'Collector Updater' -ErrorAction SilentlyContinue).Name\"")
            .Trim();
    if (svcName.Length == 0) svcName = null;

    if (svcName is null)
    {
        if (Exec("sc",
            $"create CollectorUpdater binPath= \"{svcExe}\" DisplayName= \"Collector Updater\" start= auto") == 0)
        {
            Console.WriteLine("  Collector Updater service registered.");
            svcName = "CollectorUpdater";
        }
        else
        {
            Console.WriteLine("  Failed to register Collector Updater service.");
            return;
        }
    }
    else if (svcName != "CollectorUpdater")
    {
        Console.WriteLine($"  Found existing service '{svcName}' (display name match).");
    }

    // make sure the service points at the bin copy (older installs
    // registered the publish path directly).
    Exec("sc", $"config {svcName} binPath= \"{svcExe}\"", quiet: true);

    // sc start returns before the service is fully up (and errors 1056
    // when already running) - poll state instead of checking once.
    var startOut = ExecOut("sc", $"start {svcName}");
    var running = false;
    for (var i = 0; i < 10 && !running; i++)
    {
        Thread.Sleep(1000);
        running = ExecOut("sc", $"query {svcName}").Contains("RUNNING");
    }
    if (running)
        Console.WriteLine("  Collector Updater service is running.");
    else
        Console.WriteLine($"  WARNING: could not start {svcName}.{Environment.NewLine}{startOut.Trim()}");
    if (ServiceExists(svcName) && !registered.Contains(svcName))
        registered.Add(svcName);
}

// ============================================================
//  Shared config write (replaces update-config.ps1)
// ============================================================
void ApplyWebConfig(string envSettingsPath, string feHttp, string feHttps,
    string beHttp, string beHttps, string? contentPrefix, string? ollamaUrl = null,
    string? pgHost = null, string? pgDb = null, string? pgUser = null, string? pgPass = null)
{
    Console.WriteLine();
    // Dev environment writes appsettings.json; Production writes
    // appsettings.Production.json (created if the deploy never shipped one
    // - the env file must hold the overrides or a stale copy masks them).
    var targetPath = env == "Development" ? appSettingsPath : envSettingsPath;
    Console.WriteLine($"Applying configuration to {targetPath} ...");
    if (File.Exists(targetPath) || env == "Production")
    {
        var j = LoadJsonObj(targetPath) ?? new JsonObject();
        j["Urls"] = $"http://0.0.0.0:{beHttp};https://0.0.0.0:{beHttps}";
        if (j["Auth"] is JsonObject auth)
        {
            auth["Domain"] = $"https://localhost:{feHttps}";
            if (auth["JWT"] is JsonObject jwt)
            {
                jwt["ValidIssuer"] = $"https://localhost:{feHttps}";
                jwt["ValidAudience"] = $"https://localhost:{feHttps}";
            }
        }
        if (contentPrefix is not null && j["Storage"] is JsonObject storage)
        {
            var sep = contentPrefix.StartsWith('/') ? '/' : '\\';
            var p = contentPrefix.TrimEnd('/', '\\');
            storage["Articles"] = $"{p}{sep}articles{sep}";
            storage["Journal"] = $"{p}{sep}journals{sep}";
            Console.WriteLine($"  Storage.Articles = {storage["Articles"]}");
            Console.WriteLine($"  Storage.Journal  = {storage["Journal"]}");
        }
        if (ollamaUrl is not null)
        {
            (j["Ollama"] ??= new JsonObject())["Url"] = ollamaUrl;
            Console.WriteLine($"  Ollama.Url = {ollamaUrl}");
        }
        if (pgHost is not null)
        {
            var cs = $"Host={pgHost};Database={pgDb};Username={pgUser};Password={pgPass};Keepalive=30";
            (j["ConnectionStrings"] ??= new JsonObject())["Database"] = cs;
            Console.WriteLine($"  ConnectionStrings.Database = Host={pgHost};Database={pgDb};Username={pgUser};Password=***;Keepalive=30");
        }
        File.WriteAllText(targetPath, j.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Updated {targetPath}");
        Console.WriteLine($"  Urls = http://0.0.0.0:{beHttp};https://0.0.0.0:{beHttps}");
        Console.WriteLine($"  Auth.Domain / JWT issuer = https://localhost:{feHttps}");
    }
    else Console.WriteLine($"  WARNING: {targetPath} not found - backend ports not updated");

    var apiPath = Path.Combine(webServerDir, "wwwroot", "api-base-path.json");
    if (File.Exists(apiPath))
    {
        File.WriteAllText(apiPath,
            $"{{\"https\":\"https://localhost:{beHttps}\",\"http\":\"http://localhost:{beHttp}\"}}");
        Console.WriteLine($"Updated {apiPath} (http={beHttp} https={beHttps})");
    }
    else Console.WriteLine($"  WARNING: {apiPath} not found - re-publish the client if this is a stale deploy");
}

// ============================================================
//  Router config
// ============================================================
void WriteRouterConfig(string routerDir, string? user, string? pass, List<string> instanceUrls)
{
    var path = Path.Combine(routerDir, "config.json");
    // merge into an existing config: keep Admin when creds weren't
    // re-prompted, keep unrelated keys, always rewrite Instances.
    var cfg = LoadJsonObj(path) ?? new JsonObject();
    if (user is not null && pass is not null)
        cfg["Admin"] = new JsonObject { ["Username"] = user, ["Pass"] = pass };
    var instances = new JsonArray();
    for (var i = 0; i < instanceUrls.Count; i++)
        instances.Add(new JsonObject
        {
            ["Id"] = $"Charlotte-{i + 1:000}",
            ["Url"] = instanceUrls[i],
            ["ServerName"] = Environment.MachineName,
            ["UsesCookies"] = false,
            ["Note"] = ""
        });
    (cfg["Charlotte"] as JsonObject ?? (JsonObject)(cfg["Charlotte"] = new JsonObject()))["Instances"] = instances;
    File.WriteAllText(path, cfg.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"  Router config written to {path}");
}

// ============================================================
//  Helpers
// ============================================================
string Prompt(string label, string? def = null)
{
    if (skip) return def ?? "";
    Console.Write(def is null ? $"{label}: " : $"{label} [{def}]: ");
    var v = Console.ReadLine()?.Trim() ?? "";
    return v.Length > 0 ? v : def ?? "";
}

string PromptPassword(string label, string? def = null)
{
    if (skip) return def ?? "";
    Console.Write(def is null ? $"{label}: " : $"{label} [press Enter to keep saved]: ");
    var sb = new StringBuilder();
    while (true)
    {
        var k = Console.ReadKey(intercept: true);
        if (k.Key == ConsoleKey.Enter) { Console.WriteLine(); break; }
        if (k.Key == ConsoleKey.Backspace) { if (sb.Length > 0) { sb.Length--; Console.Write("\b \b"); } continue; }
        if (!char.IsControl(k.KeyChar)) { sb.Append(k.KeyChar); Console.Write('*'); }
    }
    return sb.Length > 0 ? sb.ToString() : def ?? "";
}

bool PromptYN(string label, bool def = false)
    => Prompt($"{label} (Y/N)", def ? "Y" : "N").Equals("Y", StringComparison.OrdinalIgnoreCase);

string TranslateLocalhost(string url)
{
    var v = url.Replace("localhost", "host.docker.internal")
               .Replace("127.0.0.1", "host.docker.internal");
    if (v != url) Console.WriteLine($"  {url} -> {v} for the container.");
    return v;
}

int Exec(string file, string args, bool quiet = false)
{
    var psi = new ProcessStartInfo(file, args)
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    try
    {
        using var p = Process.Start(psi)!;
        var outTask = p.StandardOutput.ReadToEndAsync();
        var errTask = p.StandardError.ReadToEndAsync();
        p.WaitForExit();
        if (!quiet)
        {
            // relay through Console so output lands in the log tee too
            var o = outTask.Result; var e = errTask.Result;
            if (o.Length > 0) Console.Write(o);
            if (e.Length > 0) Console.Error.Write(e);
        }
        return p.ExitCode;
    }
    catch { return -1; }
}

bool ToolExists(string name) => Exec(name, "--version", quiet: true) == 0;

string ExecOut(string file, string args)
{
    var psi = new ProcessStartInfo(file, args)
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    try
    {
        using var p = Process.Start(psi)!;
        var outp = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return p.ExitCode == 0 ? outp : "";
    }
    catch { return ""; }
}

void Fail(string msg)
{
    Console.WriteLine($"  ERROR: {msg}");
    Console.WriteLine("  Press any key to exit...");
    try { Console.ReadKey(true); } catch { }
    Environment.Exit(1);
}

Dictionary<string, string> ReadEnvFile(string path)
{
    var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    try
    {
        if (!File.Exists(path)) return d;
        foreach (var line in File.ReadAllLines(path))
        {
            var eq = line.IndexOf('=');
            if (eq > 0) d[line[..eq].Trim()] = line[(eq + 1)..].Trim();
        }
    }
    catch { }
    return d;
}

Dictionary<string, string> LoadState(string path)
{
    try
    {
        return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
               ?? new Dictionary<string, string>();
    }
    catch { return new Dictionary<string, string>(); }
}

JsonObject? LoadJsonObj(string path)
{
    try
    {
        if (!File.Exists(path)) return null;
        return JsonNode.Parse(File.ReadAllText(path),
            new JsonNodeOptions(),
            new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            })?.AsObject();
    }
    catch { return null; }
}

void MergeInto(JsonObject target, JsonObject overlay)
{
    foreach (var kv in overlay)
    {
        if (kv.Value is JsonObject o && target[kv.Key] is JsonObject t)
            MergeInto(t, o);
        else
            target[kv.Key] = kv.Value?.DeepClone();
    }
}

Dictionary<string, string> ConnParts(string? connString)
{
    var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    if (connString is null) return d;
    foreach (var seg in connString.Split(';', StringSplitOptions.RemoveEmptyEntries))
    {
        var eq = seg.IndexOf('=');
        if (eq > 0) d[seg[..eq].Trim()] = seg[(eq + 1)..].Trim();
    }
    return d;
}

void SaveState(string path, Dictionary<string, string> s) =>
    File.WriteAllText(path, JsonSerializer.Serialize(s,
        new JsonSerializerOptions { WriteIndented = true }));

// converts a mapped-drive path (M:\...) to its UNC target
// (\\server\share\...) so services - which run in session 0 without
// user drive mappings - can still locate the executable. Returns the
// input unchanged for local drives, real disks and UNC paths.
string ToServicePath(string path)
{
    try
    {
        if (path.Length < 3 || path[1] != ':' || path.StartsWith(@"\\"))
            return path;
        var sb = new StringBuilder(512);
        var len = sb.Capacity;
        if (WNetGetConnection(path[..2], sb, ref len) != 0)
            return path;
        var remote = sb.ToString().TrimEnd('\\');
        return remote + path[2..];
    }
    catch { return path; }
}

[System.Runtime.InteropServices.DllImport("mpr.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
static extern int WNetGetConnection(string localName, StringBuilder remoteName, ref int length);

// mirrors console output to a log file - WriteLine/Write(string) cover
// everything setup.exe prints (prompts, statuses, relayed child output).
sealed class TeeWriter(TextWriter inner, string path) : TextWriter
{
    public override Encoding Encoding => inner.Encoding;
    public override void Write(char value)
    {
        inner.Write(value);
        try { File.AppendAllText(path, value.ToString()); } catch { }
    }
    public override void Write(string? value)
    {
        inner.Write(value);
        if (value is not null) { try { File.AppendAllText(path, value); } catch { } }
    }
    public override void WriteLine(string? value)
    {
        inner.WriteLine(value);
        try { File.AppendAllText(path, value + Environment.NewLine); } catch { }
    }
    public override void Flush() => inner.Flush();
}
