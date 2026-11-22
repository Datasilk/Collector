using Microsoft.Extensions.Hosting;
using System.Diagnostics;

// Collector.Updater - Windows service watching its own bin\ folder for
// markers written by deploy.bat:
//
// 1) `_deploying` - written BEFORE publishing. We stop the docker
//    containers (or windows services) that lock bind-mounted binaries
//    over the share, then delete it so the publish can proceed.
// 2) `_deployed` - written AFTER everything is published. App binaries
//    are bind-mounted, so a plain `docker compose restart` (or service
//    start) is all that's needed to load the new code.
// 3) `_playwright` - written by `deploy.bat --playwright`. We docker
//    exec install-browsers.sh in every running charlotte container.

// log file is created the moment the service starts - everything the
// updater does (including fatal exceptions before shutdown) lands here.
// if the exe's directory isn't writable by the service account (e.g.
// mapped drive / network share), fall back to the process temp dir so
// the failure is still recorded somewhere.
string logPath;
try
{
    var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
    Directory.CreateDirectory(logDir);
    logPath = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd-HH-mm}.log");
}
catch
{
    var logDir = Path.Combine(Path.GetTempPath(), "collector-updater");
    Directory.CreateDirectory(logDir);
    logPath = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd-HH-mm}.log");
}
void log(string m)
{
    try { File.AppendAllText(logPath, $"[{DateTime.Now:u}] {m}{Environment.NewLine}"); }
    catch { }
}
log("Started updater service");

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddWindowsService(o => o.ServiceName = "Collector Updater");
    builder.Services.AddHostedService(_ => new SetupWatcher(log));
    await builder.Build().RunAsync();
    log("updater service stopped");
}
catch (Exception ex)
{
    log($"fatal exception:{Environment.NewLine}{ex}");
    throw;
}

sealed class SetupWatcher(Action<string> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // published to {deploy}\Collector.Updater\bin\ - root is two levels up
        var root = Directory
            .GetParent(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar))?.Parent?.FullName
                   ?? AppContext.BaseDirectory;
        var setupExe = Path.Combine(root, "setup.exe");
        var deploying = Path.Combine(AppContext.BaseDirectory, "_deploying");
        var deployed = Path.Combine(AppContext.BaseDirectory, "_deployed");
        var playwright = Path.Combine(AppContext.BaseDirectory, "_playwright");
        var dockerDir = Path.Combine(root, "Deploy", "docker");
        var composeArgs = File.Exists(Path.Combine(dockerDir, "collector.yml"))
            ? string.Join(" ",
                new[] { "collector.yml", "charlotte.yml", "charlotte-router.yml" }
                    .Select(f => $"-f \"{Path.Combine(dockerDir, f)}\""))
            : null;
        // "does this deploy use docker?" - ps -aq includes STOPPED
        // containers (deploys stop them first, so a running-only check
        // would fall through to the windows-service path mid-deploy).
        bool ContainersExist()
        {
            if (composeArgs is null) return false;
            var psi = new ProcessStartInfo("docker", $"compose {composeArgs} ps -aq")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            var ids = p is null ? "" : p.StandardOutput.ReadToEnd().Trim();
            p?.WaitForExit(60_000);
            return ids.Length > 0;
        }

        // one watcher on bin\ for all markers (filter _* catches
        // _deploying/_deployed/_playwright). Seeded with Exists checks
        // in case a marker predates service start.
        var deployingSeen = File.Exists(deploying);
        var deployedSeen = File.Exists(deployed);
        var playwrightSeen = File.Exists(playwright);
        using var mfsw = new FileSystemWatcher(AppContext.BaseDirectory, "_*")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
                         | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        void OnMarker(FileSystemEventArgs e)
        {
            if (e.Name is null) return;
            if (e.Name.Equals("_deploying", StringComparison.OrdinalIgnoreCase))
                deployingSeen = true;
            else if (e.Name.Equals("_deployed", StringComparison.OrdinalIgnoreCase))
                deployedSeen = true;
            else if (e.Name.Equals("_playwright", StringComparison.OrdinalIgnoreCase))
                playwrightSeen = true;
        }
        mfsw.Created += (_, e) => OnMarker(e);
        mfsw.Changed += (_, e) => OnMarker(e);
        mfsw.Renamed += (_, e) => OnMarker(e);
        mfsw.Error += (_, e) => log("marker watcher error: " + e.GetException().Message);

        // decided once at _deploying time - containers are stopped by
        // then anyway, so re-checking at _deployed can't see them.
        // null = no _deploying seen yet (edge case: marker missed).
        bool? deployUsesDocker = null;

        var busy = false;
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(1000, ct);
            if (busy) continue;

            // deploy-start marker: stop everything that locks binaries,
            // then delete it so deploy.bat can proceed.
            if (deployingSeen && File.Exists(deploying))
            {
                deployingSeen = false;
                busy = true;
                try
                {
                    log("starting setup process...");
                    deployUsesDocker = ContainersExist();
                    if (deployUsesDocker.Value)
                        Run("docker", $"compose {composeArgs} stop", log);
                    else
                        // docker deploys don't register windows services -
                        // only sc stop when containers weren't found. Never
                        // stop CollectorUpdater - that's us.
                        foreach (var svc in new[] { "Collector", "Charlotte", "CharlottesWeb" })
                            Run("sc", $"stop {svc}", log);
                    File.Delete(deploying);
                    log("services stopped; _deploying deleted");
                }
                catch (Exception ex) { log("marker handling failed: " + ex.Message); }
                finally { busy = false; }
                continue;
            }

            // deploy-finished marker: publish is done. Binaries are
            // bind-mounted so a bounce loads the new code - no setup run
            // needed. Delete BEFORE bouncing: a stale marker would
            // retrigger if this service restarts.
            if (deployedSeen && File.Exists(deployed))
            {
                deployedSeen = false;
                busy = true;
                try
                {
                    File.Delete(deployed);
                    log("_deployed seen - bouncing containers/services");
                    if (deployUsesDocker ?? ContainersExist())
                        Run("docker", $"compose {composeArgs} restart", log);
                    else
                        foreach (var svc in new[] { "Collector", "Charlotte", "CharlottesWeb" })
                            Run("sc", $"start {svc}", log);
                    // swap in the newly published updater.exe: setup
                    // --updater stops this service, copies it into bin\
                    // and restarts us. Launch detached - we exit when
                    // the service stops, the child finishes the swap.
                    if (File.Exists(setupExe))
                    {
                        log("launching 'setup.exe --updater' to refresh bin\\updater.exe");
                        Process.Start(new ProcessStartInfo(setupExe, "--updater")
                        {
                            WorkingDirectory = root,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                    }
                }
                catch (Exception ex) { log("_deployed handling failed: " + ex.Message); }
                finally { busy = false; }
                continue;
            }

            // playwright marker: docker exec install-browsers.sh in every
            // running charlotte container (sentinels inside the script
            // make already-current installs a fast no-op).
            if (playwrightSeen && File.Exists(playwright))
            {
                playwrightSeen = false;
                busy = true;
                try
                {
                    log("_playwright seen - updating browsers in charlotte containers");
                    // /app is a bind mount of {root}\Charlotte - stage the
                    // script there so `docker exec` can reach it.
                    var src = Path.Combine(dockerDir, "install-browsers.sh");
                    var dst = Path.Combine(root, "Charlotte", "install-browsers.sh");
                    if (File.Exists(src)) File.Copy(src, dst, true);
                    var psi = new ProcessStartInfo("docker", "ps --format {{.Names}}")
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    var names = p is null ? "" : p.StandardOutput.ReadToEnd();
                    p?.WaitForExit(30_000);
                    var count = 0;
                    foreach (var name in names.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var cn = name.Trim();
                        if (!cn.StartsWith("collector-charlotte-", StringComparison.OrdinalIgnoreCase))
                            continue;
                        count++;
                        log($"installing playwright browsers in {cn} - first run takes a few minutes");
                        Run("docker", $"exec {cn} bash /app/install-browsers.sh", log, 30 * 60_000);
                    }
                    if (count == 0) log("no running charlotte containers found");
                    File.Delete(playwright);
                    log($"_playwright deleted ({count} container(s) updated)");
                }
                catch (Exception ex) { log("_playwright handling failed: " + ex.Message); }
                finally { busy = false; }
                continue;
            }
        }
    }

    static int Run(string file, string args, Action<string> log, int timeoutMs = 120_000)
    {
        try
        {
            var psi = new ProcessStartInfo(file, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p is null) return -1;
            var outTask = p.StandardOutput.ReadToEndAsync();
            var errTask = p.StandardError.ReadToEndAsync();
            p.WaitForExit(timeoutMs);
            var outp = outTask.Result.Trim();
            var err = errTask.Result.Trim();
            if (outp.Length > 0 && !outp.Contains("not been started"))
                log($"{file} {args} -> exit {p.ExitCode}:{Environment.NewLine}{outp}");
            if (err.Length > 0)
                log($"{file} {args} stderr:{Environment.NewLine}{err}");
            return p.ExitCode;
        }
        catch (Exception ex) { log($"{file} {args} failed: {ex.Message}"); return -1; }
    }
}
