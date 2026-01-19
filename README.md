# EditorConfig Language Service

[![Build](https://github.com/madskristensen/EditorConfigLanguage/actions/workflows/build.yaml/badge.svg)](https://github.com/madskristensen/EditorConfigLanguage/actions/workflows/build.yaml)
[![Visual Studio Marketplace Version](https://img.shields.io/visual-studio-marketplace/v/MadsKristensen.EditorConfig)](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.EditorConfig)
[![Visual Studio Marketplace Downloads](https://img.shields.io/visual-studio-marketplace/d/MadsKristensen.EditorConfig)](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.EditorConfig)
[![Visual Studio Marketplace Rating](https://img.shields.io/visual-studio-marketplace/r/MadsKristensen.EditorConfig)](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.EditorConfig)

Download this extension from the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.EditorConfig)
or get the [CI build](https://www.vsixgallery.com/extension/1209461d-57f8-46a4-814a-dbe5fecef941/).

---------------------------------------

**Provides full language support for .editorconfig files in Visual Studio, including IntelliSense, validation, and formatting.**

[The EditorConfig Project](https://editorconfig.org/) helps developers define and maintain consistent coding styles between different editors and IDEs.

Visual Studio natively supports .editorconfig files, but it doesn't provide rich language support for editing those files. This extension fills that gap with IntelliSense, validation, formatting, and more.

> **Note:** This extension requires Visual Studio 2022 (17.0) or newer.

See the [change log](CHANGELOG.md) for changes and road map.

Need help getting started? Check out [Microsoft's EditorConfig documentation](https://learn.microsoft.com/visualstudio/ide/create-portable-custom-editor-options) for details and examples of coding styles available.

## Features

- Makes it easy to create .editorconfig files
- Syntax highlighting
- C#, VB.NET, and C++ style analyzers
- Intellisense
- Code snippets
- Validation
- Hover tooltips
- Light bulbs
- Code formatting
- Navigational drop downs
- Inheritance visualizer
- Settings
- Brace completion
- Brace matching
- Comment/uncomment
- Outlining (code folding)
- Drag 'n drop file onto .editorconfig file

### Create .editorconfig Files
To make it really easy to add a .editorconfig file, you can now right-click
any folder, project, solution folder and hit **Add -> .editorconfig File**

![Classification](art/context-menu.png)

### Syntax Highlighting
Full colorization of the full .editorconfig syntax.

![Classification](art/classification.png)

### C#, VB.NET, and C++ Style Analyzers
Visual Studio lets you add C#, VB.NET, and C++ specific rules to the .editorconfig file. In addition to enabling various rules, a severity is also added to control how Visual Studio is going to handle these rules.

![C# and .NET style analyzers](art/csharp-analyzers.png)

Each severity is clearly marked by an icon to make it easy to identify.

#### C++ Support
The extension provides full IntelliSense and validation for [C++ EditorConfig properties](https://learn.microsoft.com/visualstudio/ide/cpp-editorconfig-properties), including indentation, formatting, and code style options.

### Intellisense
The extension provides Intellisense for both keywords and values.

![Classification](art/keyword-intellisense.png)  

![Classification](art/value-intellisense.png)

### Code Snippets
Various code snippets have been added to make it easier to work with .editorconfig files.

To insert a snippet, right-click inside the editor or hit *Ctrl+K,Ctrl+X*.

![Snippets](art/snippets-context-menu.png)

This will show a list of available snippets to insert.

![Snippets](art/snippets-expansion.png)

### Validation
Error squiggles are shown for invalid values.

![Classification](art/validation.png)

Properties that are being overridden by a duplicate property in the same section is easy to identify.

![Validate duplicates](art/validation-duplicates.png)

If a parent document contains the exact same property and value in a section with the same globbing pattern, a suggestion shows up to remove it.

![Validate parent](art/validation-duplicates-parent.png)

See the [complete list of error codes](https://github.com/madskristensen/EditorConfigLanguage/wiki/Error-codes).

To suppress any error in the .editorconfig document, use the light bulb feature:

![Suppress error](art/suppress_error.png)

That will add a special comment at the top of the file to let the validator know what error messages to suppress.

![Suppress Comment](art/suppress-comment.png)

Another way to suppress the error is by right-clicking the error in the Error List.

![Suppress from Error list](art/suppress-errorlist.png)

#### Third-Party Property Support
Many third-party tools like ReSharper, Rider, and Roslynator add their own properties to .editorconfig files. By default, the extension ignores properties with these common prefixes to avoid false validation errors:

- `resharper_` - JetBrains ReSharper/Rider properties
- `idea_` - JetBrains IntelliJ IDEA properties
- `roslynator_` - Roslynator analyzer properties
- `ij_` - Alternative IntelliJ prefix

You can customize the ignored prefixes in **Tools → Options → Text Editor → EditorConfig → Validation → Ignored property prefixes**.

### Hover Tooltips
Hover the mouse over any keyword to see a full description.

![Classification](art/quick-info.png)

### Light Bulbs
Sorting properties, deleting sections, and adding missing rules is easy with the commands being shown as light bulbs in the editor margin.

![Light bulbs](art/light-bulb.png)

### Code Formatting
Typing `Ctrl+K,D` will invoke Visual Studio's *Format Document* command. By default that will align all the equal (`=`) delimeters and add 1 space character around both equal- and colon characters. This behavior is configurable in the settings (see below).

![Code formatting](art/formatting.png)

### Navigational Drop Downs
Dropdown menus at the top of the editor makes it easy to navigate the document.

![Navigational drop downs](art/navigation-dropdown.png)

### Inheritance Visualizer
A project can have multiple .editorconfig files and the rules in each cascades
from the top-most and down. It is based on folder structure.

The inheritance visualizer is located at the bottom right corner of the editor window and makes it easy to see this relationship.

![Inheritance visualizer](art/inheritance-visualizer.png)

You can navigate to the immediate parent document by hitting **F12**. You can change the shortcut under Tools -> Options -> Environment -> Keyboard and find the command called *EditorConfig.NavigateToParent*.

Note, the inheritance visualizer is only visible when the current file isn't the root of the hierarchy or by specifying the `root = true` property.

### Settings
Change the behavior of the editor from **Tools -> Options** or simply by right-clicking in the editor.

![Open EditorConfig settings](art/editor-context-menu.png)

![Settings](art/settings.png)

## Extensibility

### Custom Schema Support for Extension Authors
Other Visual Studio extensions can contribute their own EditorConfig properties that will be recognized by IntelliSense and validation. This is useful for extensions that introduce custom analyzer rules or tool-specific settings.

To register a custom schema, add the following to your extension's `.pkgdef` file:

```pkgdef
[$RootKey$\Languages\Language Services\EditorConfig\Schemas]
"MyExtensionName"="$PackageFolder$\my-editorconfig-schema.json"
```

The schema JSON file should follow the same format as the built-in schema, with a `"properties"` array:

```json
{
  "properties": [
    {
      "name": "my_custom_property",
      "description": "Description shown in tooltips and IntelliSense.",
      "values": ["value1", "value2", "value3"],
      "defaultValue": ["value1"],
      "severity": false,
      "documentationLink": "https://example.com/docs/my-property"
    },
    {
      "name": "my_severity_property",
      "description": "A property that supports severity suffixes.",
      "values": [true, false],
      "defaultValue": [true],
      "severity": true,
      "defaultSeverity": "warning"
    }
  ]
}
```

#### Schema Property Fields

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | **Required.** The property name (e.g., `my_tool_option`). |
| `description` | string | Description shown in tooltips and IntelliSense. |
| `values` | array | List of valid values (strings, booleans, or numbers). |
| `defaultValue` | array | The default value(s) for the property. |
| `severity` | boolean | If `true`, the property supports severity suffixes (`:error`, `:warning`, etc.). |
| `defaultSeverity` | string | Default severity when `severity` is `true`. One of: `none`, `silent`, `suggestion`, `warning`, `error`. |
| `documentationLink` | string | URL to documentation for F1 help. |
| `multiple` | boolean | If `true`, the value can be a comma-separated list. |
| `hidden` | boolean | If `true`, the property won't appear in IntelliSense but will be recognized. |
| `unsupported` | boolean | If `true`, the property is marked as not supported by Visual Studio. |
| `example` | string | Code example showing the effect of this property. |

#### Precedence Rules

- **Built-in properties always take precedence** over custom properties with the same name.
- If multiple extensions register properties with the same name, the first one loaded wins.
- **Severities cannot be added or modified** by custom schemas; only the built-in severities are used.

### Contribute
To build this project locally:

1. Clone the repository
2. Open `EditorConfigLanguage.slnx` in Visual Studio 2022
3. Build and run (F5) to test in the Experimental Instance

References to available formatting/code options directly from Roslyn codebase:
- [CSharp Formatting Options](https://github.com/dotnet/roslyn/blob/main/src/Workspaces/CSharp/Portable/Formatting/CSharpFormattingOptions.cs)
- [CSharp Code Style Options](https://github.com/dotnet/roslyn/blob/main/src/Workspaces/CSharp/Portable/CodeStyle/CSharpCodeStyleOptions.cs)
- [.NET Formatting Options](https://github.com/dotnet/roslyn/blob/main/src/Workspaces/Core/Portable/Formatting/FormattingOptions.cs)
- [.NET Code Style Options](https://github.com/dotnet/roslyn/blob/main/src/Workspaces/Core/Portable/CodeStyle/CodeStyleOptions2.cs)

### License
[Apache 2.0](LICENSE)