# EntityConfigurationGenerator

## Purpose
This project is intended to create extensibulity to allow rapid creation of configuration files for your data models.
This extension works on a single file or a folder.

## Installing
Download, clean, and rebuild tghe project to create the vsix installer. 
Go to the project/bin/{debug/release} folder and double click the .vsix file to trigger the install

## Configuring
There's only one parameter configurable for the extension - and that's to prefer partials or not. Standard boolean value. 

### Generate As Partial = true
This will set the extension to generate partial classes for the config classes and convert your original class into a partial.
Then it will generate the configuration in the same folder as {classname}.config.cs as a partial. 
You'll find it by expanding your class's caret next to it.
#### Generated Tree
<pre>
Data/
├── Models/
│   ├── Customer.cs
│   ├── Customer.config.cs
</pre>
### Generate As Partial = false
This will create a "Configurations" folder as a sibling of your class's containing folder and create the files in the appropriate namespace.
It will also add a reference to the class's original namespace.
#### Generated Tree
<pre>
Data/
├── Models/
│   └── Customer.cs (only converts to partial)
├── Configurations/
│   └── CustomerConfiguration.cs
</pre>


# File Generation Template
The extension will create a templated file depending on the settings of the extension. 
If use partials is false, then {usingEntityNamespace} will be the original namespace from the orifginal class - 
otherwise this will be an empty string since the namespace scope does not change for partial classes.

```cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
{usingEntityNamespace}
namespace {fileNamespace}
{
	public {partialKeyword}class  {newClassName} : IEntityTypeConfiguration<{className}>
	{
		public void Configure(EntityTypeBuilder<{className}> builder)
		{
		}
	}
};
```
If partials are used then newclassname = className, otherwise, newClassName = classNameConfiguration
