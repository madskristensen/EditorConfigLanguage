# EditorConfig

<!-- Replace this badge with your own-->
[![Build status](https://ci.appveyor.com/api/projects/status/hv6uyc059rqbc6fj?svg=true)](https://ci.appveyor.com/project/madskristensen/extensibilitytools)

<!-- Update the VS Gallery link after you upload the VSIX-->
Download this extension from the [VS Gallery](https://visualstudiogallery.msdn.microsoft.com/a8c00bab-9ef3-47a4-8aaa-802d5cdb6ec0)
or get the [CI build](http://vsixgallery.com/extension/1209461d-57f8-46a4-814a-dbe5fecef941/).

---------------------------------------

[The EditorConfig Project](http://editorconfig.org/) helps developers define and maintain
consistent coding styles between different editors and IDEs.

Visual Studio 2017 natively supports .editorconfig files, but it doesn't give
language support for editing those files. This extension provides that.

See the [change log](CHANGELOG.md) for changes and road map.

## Features

- Makes it easy to create .editorconfig files
- Syntax highlighting
- Intellisense
- Validation
- Brace completion
- Brace matching
- Comment/uncomment
- Outlining (code folding)
- Drag 'n drop file onto .editorconfig file
- Code formatting
- Hover tooltips

### Create .editorconfig files
To make it really easy to add a .editorconfig file, you can now right-click
any folder, project, solution folder and hit **Add -> .editorconfig File**

![Classification](art/context-menu.png)

### Syntax highlighting

![Classification](art/classification.png)

### Intellisense
The extension provides Intellisense for both keywords and values.

![Classification](art/keyword-intellisense.png)  

![Classification](art/value-intellisense.png)

### Validation
Red squiggles are shown for invalid values.

![Classification](art/validation.png)

### Hover tooltips
Hover the mouse over any keyword to see a full description.

![Classification](art/quick-info.png)

## Contribute
Check out the [contribution guidelines](.github/CONTRIBUTING.md)
if you want to contribute to this project.

For cloning and building this project yourself, make sure
to install the
[Extensibility Tools 2015](https://visualstudiogallery.msdn.microsoft.com/ab39a092-1343-46e2-b0f1-6a3f91155aa6)
extension for Visual Studio which enables some features
used by this project.

## License
[Apache 2.0](LICENSE)