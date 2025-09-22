// ModDependencyResolver.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bohemia_Solutions
{
    public static class ModDependencyResolver
    {
        // Výchozí cesta na workshop (můžeš si přepsat dle potřeby)
        public static string DefaultWorkshopRoot =
            @"C:\Program Files (x86)\Steam\steamapps\common\DayZ\!Workshop";

        // Normalizace názvu (bez @, bez diakritiky/oddělovačů, lower)
        private static string Norm(string name)
            => Regex.Replace(name?.Trim().TrimStart('@') ?? "", @"[^a-z0-9]+", "").ToLowerInvariant();

        // --- čtení dependencies[] z meta.cpp (privátní parser) ---
        private static IEnumerable<string> ParseDependenciesFromMeta(string metaPath)
        {
            if (!File.Exists(metaPath)) yield break;

            var text = File.ReadAllText(metaPath);
            var m = Regex.Match(text, @"dependencies\s*\[\s*\]\s*=\s*\{(?<list>.*?)\};",
                                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!m.Success) yield break;

            foreach (Match q in Regex.Matches(m.Groups["list"].Value, "\"([^\"]+)\""))
            {
                var dep = q.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(dep))
                    yield return dep;
            }
        }

        // Veřejný helper – přesně to, co voláš z ConfigForm:
        // vrátí dependencies[] z @<mod>\meta.cpp v daném rootu.
        public static List<string> ReadMetaDependencies(string workshopRoot, string modFolder)
        {
            workshopRoot ??= DefaultWorkshopRoot;
            var meta = Path.Combine(workshopRoot, modFolder ?? "", "meta.cpp");
            return ParseDependenciesFromMeta(meta).ToList();
        }

        private static HashSet<string> KnownModFolders(string workshopRoot)
        {
            if (!Directory.Exists(workshopRoot)) return new();
            return Directory.GetDirectories(workshopRoot)
                            .Select(Path.GetFileName)
                            .Where(n => !string.IsNullOrWhiteSpace(n))
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        // --- tvrdá pravidla (chart) ---
        private static bool IsCF(string m) => Norm(m) == "cf";
        private static bool IsCOT(string m) => Norm(m) == "communityonlinetools";
        private static bool IsDabs(string m) => Norm(m) == "dabsframework" || Norm(m) == "df";
        private static bool IsExpCore(string m) => Norm(m).Contains("expansioncore");
        private static bool IsExpAny(string m) => Norm(m).StartsWith("dayzexpansion") || Norm(m).StartsWith("expansion");
        private static bool IsMapAssets(string m)
            => Norm(m).Contains("expansionmapassets") || Norm(m).Contains("builderitems"); // ← Contains s velkým C

        private static int RankChart(string m)
        {
            if (IsCF(m)) return 0; // @CF
            if (IsCOT(m)) return 1; // @Community-Online-Tools
            if (IsDabs(m)) return 2; // @Dabs Framework
            if (IsExpCore(m)) return 3; // @DayZ-Expansion-Core
            if (IsExpAny(m) && !IsMapAssets(m)) return 4; // ostatní EXP
            if (IsMapAssets(m)) return 5; // Map-Assets / BuilderItems
            return 6;                                                  // vše ostatní
        }

        // Stabilní „chart“ řazení – sekundární klíč je původní pořadí
        private static List<string> StableChartSort(IReadOnlyList<string> items)
        {
            var idx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < items.Count; i++) idx[items[i]] = i;

            return items
                .OrderBy(m => RankChart(m))
                .ThenBy(m => idx[m])
                .ToList();
        }

        // Přidá hranu dep -> mod (dep MUSÍ být před modem)
        private static void AddEdge(
            Dictionary<string, HashSet<string>> adj,
            Dictionary<string, int> indeg,
            string depKey, string modKey)
        {
            if (depKey == modKey) return;
            if (!adj.ContainsKey(depKey)) adj[depKey] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!indeg.ContainsKey(depKey)) indeg[depKey] = 0;
            if (!indeg.ContainsKey(modKey)) indeg[modKey] = 0;

            if (adj[depKey].Add(modKey))
                indeg[modKey] = indeg.TryGetValue(modKey, out var v) ? v + 1 : 1;
        }

        /// <summary>
        /// Vrací seřazené mod složky. Kombinuje topologii z meta.cpp a tvrdá pravidla (CF→COT→Dabs→Core→EXP).
        /// </summary>
        public static List<string> ResolveOrder(
            IEnumerable<string> selectedMods,
            string workshopRoot,
            out List<string> missingDependencies,
            out List<(string Mod, string DependsOn)> cycles)
        {
            missingDependencies = new();
            cycles = new();

            workshopRoot ??= DefaultWorkshopRoot;

            var input = selectedMods?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                         ?? new List<string>();

            // zachovej první výskyt + mapování norm->originál
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var unique = input.Where(seen.Add).ToList();

            var normToOriginal = unique
                .GroupBy(m => Norm(m))
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var knownFolders = KnownModFolders(workshopRoot);

            // graf dep -> mod
            var adj = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var indeg = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var m in unique)
            {
                var k = Norm(m);
                if (!adj.ContainsKey(k)) adj[k] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!indeg.ContainsKey(k)) indeg[k] = 0;
            }

            // 1) hrany z meta.cpp
            foreach (var original in unique)
            {
                var key = Norm(original);
                var meta = Path.Combine(workshopRoot, original, "meta.cpp");
                var deps = ParseDependenciesFromMeta(meta)
                           .Select(Norm)
                           .Where(d => !string.IsNullOrWhiteSpace(d))
                           .ToList();

                foreach (var dep in deps)
                {
                    var depIsSelected = normToOriginal.ContainsKey(dep);
                    if (!depIsSelected)
                    {
                        var maybeFolder = knownFolders.FirstOrDefault(f => Norm(f) == dep);
                        if (!string.IsNullOrEmpty(maybeFolder))
                            missingDependencies.Add(normToOriginal[key] + " → @" + maybeFolder);
                        else
                            missingDependencies.Add(normToOriginal[key] + " → " + dep);
                        continue;
                    }

                    AddEdge(adj, indeg, dep, key);
                }
            }

            // 2) tvrdé hrany dle chartu
            string? cf = unique.FirstOrDefault(IsCF);
            string? cot = unique.FirstOrDefault(IsCOT);
            string? dabs = unique.FirstOrDefault(IsDabs);
            string? core = unique.FirstOrDefault(IsExpCore);

            string K(string? x) => x is null ? "" : Norm(x);

            if (cf != null)
            {
                foreach (var m in unique)
                    if (!IsCF(m)) AddEdge(adj, indeg, K(cf), Norm(m));
            }

            if (cot != null)
            {
                if (cf != null) AddEdge(adj, indeg, K(cf), K(cot));
                foreach (var m in unique)
                    if (!IsCF(m) && !IsCOT(m)) AddEdge(adj, indeg, K(cot), Norm(m));
            }

            if (dabs != null)
            {
                if (cot != null) AddEdge(adj, indeg, K(cot), K(dabs));
                foreach (var m in unique)
                    if (IsExpAny(m)) AddEdge(adj, indeg, K(dabs), Norm(m));
            }

            if (core != null)
            {
                foreach (var m in unique)
                    if (IsExpAny(m) && !IsExpCore(m)) AddEdge(adj, indeg, K(core), Norm(m));
            }

            // 3) topologické řazení (Kahn)
            var q = new Queue<string>(indeg.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            var topo = new List<string>();

            while (q.Count > 0)
            {
                var u = q.Dequeue();
                topo.Add(u);

                if (!adj.TryGetValue(u, out var outs)) continue;
                foreach (var v in outs)
                {
                    indeg[v]--;
                    if (indeg[v] == 0) q.Enqueue(v);
                }
            }

            // 4) případné cykly jen nahlásíme a doplníme zbylé uzly
            if (topo.Count < indeg.Count)
            {
                var remaining = indeg.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
                foreach (var r in remaining)
                {
                    if (!adj.TryGetValue(r, out var outs)) continue;
                    foreach (var v in outs)
                        if (indeg[v] > 0)
                            cycles.Add((normToOriginal.GetValueOrDefault(r, r),
                                        normToOriginal.GetValueOrDefault(v, v)));
                }
                foreach (var r in remaining)
                    if (!topo.Contains(r)) topo.Add(r);
            }

            // 5) převod na originální jména + finální stabilní chart-sort
            var ordered = topo
                .Select(n => normToOriginal.TryGetValue(n, out var o) ? o : n)
                .ToList();

            ordered = StableChartSort(ordered);
            return ordered;
        }
    }
}
