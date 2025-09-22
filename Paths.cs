using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Bohemia_Solutions
{
    static class Paths
    {
        public static string InstallDir => AppContext.BaseDirectory;

        public static string UserConfigDir
        {
            get
            {
                var asm = Assembly.GetExecutingAssembly();
                var company = asm.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company
                              ?? "BohemiaSolutions";
                var product = asm.GetCustomAttribute<AssemblyProductAttribute>()?.Product
                              ?? Application.ProductName.Replace(' ', '_');

                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    company, product);

                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static string InstallChangeLog => Path.Combine(InstallDir, "changelog.json");
        public static string UserChangeLog => Path.Combine(UserConfigDir, "changelog.json");

        // <<< TADY JE TEN „KLÍČ“ / FLAG >>>
        public static string UserAdminFlag => Path.Combine(UserConfigDir, "changelog_admin.flag");
    }
}
