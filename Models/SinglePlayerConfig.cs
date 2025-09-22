using System;
using System.Collections.Generic;
using WindowsFormsApp1.Models;

namespace Bohemia_Solutions.Models
{
    public class SinglePlayerConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        // 1) Name: txtNameSP
        public string Name { get; set; } = "My SP config";

        // 2) Version: cmbVersionSP (stejný klíč/verze jako u MP)
        public string VersionFolder { get; set; } = "";   // např. "DayZ", "DayZ Server Exp", "DayZ Internal"

        // Z vybrané verze dopočítáme cesty (client/server rooty si držíme pro sken misi)
        public string ClientPath { get; set; } = "";      // kořen se spustitelným EXE
        public string ServerPath { get; set; } = "";      // kvůli \Server\mpmissions\ skenu

        // 3) Type: cmbTypeSP ("Vanilla" / "Modded")
        public string Type { get; set; } = "Vanilla";

        // 4) Exe Name: cmbExeName
        public string ExeName { get; set; } = "DayZ_x64.exe";

        // 5) Profiles Folder: txtProfilesFolder (+ „…“)
        public string ProfilesFolder { get; set; } = "";

        // 6) SP mission: cb_chooseSP (z Client\missions a Server\mpmissions)
        //    - MissionParam jde přímo do -mission= (např. missions\ce.chernarusplus)
        //    - MissionAbsPath držíme pro práci se soubory (init.c, otevření složky, backup…)
        public string MissionParam { get; set; } = "";
        public string MissionAbsPath { get; set; } = "";


        // 9) Client parameters – použijeme string z CLB (jako v MP)
        public string ClientArguments { get; set; } = "";

        // 10) Mods pro "Modded": clbWorkshopModsSP (absolutní cesty)
        public List<string> Mods { get; set; } = new();
        public string IngameName { get; set; } = "";
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime Updated { get; set; } = DateTime.Now;
        public List<string> Filters { get; set; } = new();
    }
}
