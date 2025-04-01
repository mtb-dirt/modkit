using System.Text.RegularExpressions;

namespace Modkit.Editor.Semver
{
    public class Semver
    {
        public static bool Validate(string value)
        {
            var match = Regex.Match(value, "^(0|[1-9]\\d*)\\.(0|[1-9]\\d*)\\.(0|[1-9]\\d*)(?:-((?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\\.(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\\+([0-9a-zA-Z-]+(?:\\.[0-9a-zA-Z-]+)*))?$");
            return match.Success;
        }
    }
}
