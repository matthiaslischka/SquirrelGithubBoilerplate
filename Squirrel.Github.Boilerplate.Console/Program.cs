using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Squirrel.Github.Boilerplate.Console
{
    internal class Program
    {
        public static ManualResetEvent UpdateCheckResetEvent = new ManualResetEvent(false);

        private static void Main()
        {
            EnsureAppUpToDate().ContinueWith(t => System.Console.Error.WriteLine(t.Exception),
                TaskContinuationOptions.OnlyOnFaulted);
            
            DisplayCurrentVersion();
            System.Console.ReadKey();

            UpdateCheckResetEvent.WaitOne();
        }

        private static void DisplayCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            var version = fileVersionInfo.FileVersion;
            System.Console.WriteLine(version);
        }

        private static async Task EnsureAppUpToDate()
        {
            var assembly = Assembly.GetEntryAssembly();
            var updateDotExe = Path.Combine(Path.GetDirectoryName(assembly.Location), "..", "Update.exe");
            var isInstalled = File.Exists(updateDotExe);

            //so you can run app from Dev Environment
            if (!isInstalled)
                return;

            var updated = false;

            using (var mgr = UpdateManager
                .GitHubUpdateManager("https://github.com/matthiaslischka/SquirrelGithubBoilerplate").Result)
            {
                var updateInfo = await mgr.CheckForUpdate();
                if (updateInfo.ReleasesToApply.Any())
                {
                    System.Console.Out.WriteLine($"Found Update {updateInfo.FutureReleaseEntry.Version}.");
                    await mgr.UpdateApp(i => System.Console.Out.WriteLine($"Updating: {i}"));
                    System.Console.Out.WriteLine("Update Finished.");
                    updated = true;
                }
            }

            if (updated)
            {
                System.Console.Out.WriteLine("Restarting to launch new Version.");
                UpdateManager.RestartApp();
            }

            UpdateCheckResetEvent.Set();
        }
    }
}