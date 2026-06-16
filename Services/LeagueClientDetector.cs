using LoLClientTool.Mvc.Services;
using System.Diagnostics;

namespace LoLClientTool.Services
{
    public class LeagueClientDetector : ILeagueClientDetector
    {
        private const string LeagueClientProcessName = "LeagueClientUx";
        private const string LockfileName = "lockfile";

        public bool IsLeagueClientRunning()
        {
            var processes = Process.GetProcessesByName(LeagueClientProcessName);

            return processes.Any();
        }

        public LeagueClientConnection? GetConnection()
        {
            var processes = Process.GetProcessesByName(LeagueClientProcessName);

            var leagueProcess = processes.FirstOrDefault();

            if (leagueProcess == null)
            {
                return null;
            }

            string? leagueClientFolder = GetLeagueClientFolder(leagueProcess);

            if (string.IsNullOrWhiteSpace(leagueClientFolder))
            {
                return null;
            }

            string lockfilePath = Path.Combine(leagueClientFolder, LockfileName);

            if (!File.Exists(lockfilePath))
            {
                return null;
            }

            string? lockfileContent = ReadLockfileContent(lockfilePath);

            if (string.IsNullOrWhiteSpace(lockfileContent))
            {
                return null;
            }

            return ParseLockfile(lockfileContent);
        }

        private static string? GetLeagueClientFolder(Process leagueProcess)
        {
            try
            {
                return Path.GetDirectoryName(leagueProcess.MainModule?.FileName);
            }
            catch
            {
                return null;
            }
        }

        private static string? ReadLockfileContent(string lockfilePath)
        {
            try
            {
                using var fileStream = new FileStream(
                    lockfilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);

                using var reader = new StreamReader(fileStream);

                return reader.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }

        private static LeagueClientConnection? ParseLockfile(string lockfileContent)
        {
            string[] parts = lockfileContent.Split(':');

            if (parts.Length < 5)
            {
                return null;
            }

            bool portParsed = int.TryParse(parts[2], out int port);

            if (!portParsed)
            {
                return null;
            }

            return new LeagueClientConnection
            {
                Port = port,
                Password = parts[3],
                Protocol = parts[4]
            };
        }
    }
}