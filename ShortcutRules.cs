using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Bohemia_Solutions.Models; // ShortcutItem

namespace Bohemia_Solutions
{
    /// <summary>
    /// Smart behaviors for specific tools (Tracy, RemoteDebugger, ...).
    /// </summary>
    public static class ShortcutRules
    {
        // Context from Form1 – server state / config type
        public sealed class ShortcutContext
        {
            public bool MpRunning { get; init; }
            public string? MpCfgType { get; init; }
        }

        /// <summary>
        /// Should this shortcut be enabled in UI?
        /// </summary>
        public static bool IsAllowed(ShortcutItem sc, ShortcutContext ctx)
        {
            var exe = (Path.GetFileName(sc.ExePath) ?? "").ToLowerInvariant();

            // RemoteDebugger: only when MP is running AND cfg type is Vanilla
            if (exe.Contains("remotedebugger"))
                return ctx.MpRunning &&
                       string.Equals(ctx.MpCfgType?.Trim(), "Vanilla", StringComparison.OrdinalIgnoreCase);

            // Tracy: no gating (only special handling)
            if (exe.Contains("tracy"))
                return true;

            // default: allow
            return true;
        }

        /// <summary>
        /// Try special handling for a shortcut.
        /// Returns true if handled (default launch should NOT run).
        /// </summary>
        public static bool TryHandle(ShortcutItem sc, IWin32Window? owner, ShortcutContext ctx)
        {
            var exeName = (Path.GetFileName(sc.ExePath) ?? "").ToLowerInvariant();

            if (exeName.Contains("tracy"))
                return HandleTracy(sc, owner);

            if (exeName.Contains("remotedebugger"))
                return HandleRemoteDebugger(sc, owner, ctx);

            return false; // no special behavior -> let default launch run
        }

        // ——— Rules ———

        private static bool HandleTracy(ShortcutItem sc, IWin32Window? owner)
        {
            var exePath = sc.ExePath;
            var folder = Path.GetDirectoryName(exePath);

            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                MessageBox.Show(owner,
                    "Tracy folder could not be found.\nPlease verify the path in Path Setup.",
                    "Tracy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return true;
            }

            // 1) Nech uživatele potvrdit – tohle blokuje až do kliknutí na OK
            MessageBox.Show(owner,
                "Tracy.exe is not launched automatically (to avoid running elevated).\n\n" +
                "Click OK to open the folder. Then double-click Tracy.exe manually.",
                "Manual Launch Required", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // 2) AŽ TEĎ otevři Průzkumníka – ideálně se selekcí souboru
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{exePath}\"", // zvýrazní Tracy.exe
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner,
                    $"Unable to open the Tracy folder:\n{ex.Message}",
                    "Tracy", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return true;
        }


        private static bool HandleRemoteDebugger(ShortcutItem sc, IWin32Window? owner, ShortcutContext ctx)
        {
            // 1) Gate: only if MP running + Vanilla
            if (!IsAllowed(sc, ctx))
            {
                MessageBox.Show(owner,
                    "RemoteDebugger can be launched only when the MP server is running and the config type is 'Vanilla'.",
                    "RemoteDebugger – Conditions not met",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }

            // 2) Validate EXE
            if (!File.Exists(sc.ExePath))
            {
                MessageBox.Show(owner,
                    "RemoteDebugger.exe not found:\n" + sc.ExePath,
                    "RemoteDebugger", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }

            // 3) Dynamic WorkingDirectory (prefer stored WorkingDir; fallback to EXE directory)
            var wd =
                (!string.IsNullOrWhiteSpace(sc.WorkingDir) && Directory.Exists(sc.WorkingDir))
                    ? sc.WorkingDir
                    : (Path.GetDirectoryName(sc.ExePath) ?? Environment.CurrentDirectory);

            var psi = new ProcessStartInfo
            {
                FileName = sc.ExePath,
                WorkingDirectory = wd,
                UseShellExecute = true,
                CreateNoWindow = true,
                Arguments = sc.Arguments ?? ""
            };

            try { Process.Start(psi); }
            catch (Exception ex)
            {
                MessageBox.Show(owner,
                    "Failed to launch RemoteDebugger:\n" + ex.Message,
                    "RemoteDebugger", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return true;
        }
    }
}
