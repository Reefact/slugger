namespace Slugger.Cli.CommandLine;

/// <summary>What the command line asked slugger to do.</summary>
internal enum CliCommand
{
    /// <summary>Draw slugs, in a REPL or once. What slugger does when no command says otherwise.</summary>
    Generate,

    /// <summary>List the themes in scope, then exit.</summary>
    ListThemes,

    /// <summary>Persist the rest of the line as the defaults of future runs, then exit.</summary>
    SaveDefaults,

    /// <summary>Validate a theme file and copy it into the theme directory, then exit.</summary>
    Register,

    /// <summary>Measure a theme file and write the report beside it (<c>--analyze</c>).</summary>
    Analyze,

    /// <summary>Delete a custom theme, then exit.</summary>
    Unregister,

    /// <summary>Show a theme's own "meta" block, then exit (<c>--theme-info</c>).</summary>
    ThemeInfo,
}
