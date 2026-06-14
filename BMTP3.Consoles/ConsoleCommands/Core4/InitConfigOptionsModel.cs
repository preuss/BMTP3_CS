using System.CommandLine;

namespace BMTP3.Consoles.ConsoleCommands.Core4;

public class InitConfigOptionsModel : BaseOptionsModel
{
    public static Option<FileInfo> OutputOption { get; } = new("--output", "-o")
    {
        Description = "Output path: directory or filename. Extension determines format: .toml, .json, or .json5. Default: default.toml.",
        Arity = ArgumentArity.ZeroOrOne
    };

    public FileInfo? Output { get; set; }
}
