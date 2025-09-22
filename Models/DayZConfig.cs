using System;
using System.Collections.Generic;

namespace WindowsFormsApp1.Models
{
    public class DayZConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // "Vanilla" nebo "Modding"
        public string VersionFolder { get; set; }
        public string ServerPath { get; set; }
        public string ClientPath { get; set; }
        public ServerParams ServerParameters { get; set; }
        public ClientParams ClientParameters { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public List<string> Filters { get; set; } = new();

        public string ServerName { get; set; }
    }

    public class ServerParams
    {
        public string ConfigFile { get; set; }
        public string ProfilesFolder { get; set; }
        public int Port { get; set; }
        public string AdditionalParams { get; set; }
        public List<string> Mods { get; set; }
        public int CpuCount { get; set; }                     // NOVÉ – počet jader
        public string ExeName { get; set; }                   // NOVÉ – spouštěný exe
    }


    public class ClientParams
    {
        // PŮVODNÍ – necháváme kvůli kompatibilitě MP
        public string Arguments { get; set; } = "";

        // NOVÉ – pro „checkboxy“ v Client parameters editoru
        public List<ClientParamItem> List { get; set; } = new();
    }

    public class ClientParamItem
    {
        public bool Enabled { get; set; }
        public string Value { get; set; } = "";
    }

}
