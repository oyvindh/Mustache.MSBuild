# Mustache.MSBuild

[![License](https://img.shields.io/github/license/oyvindh/Mustache.MSBuild.svg?color=blue)](https://github.com/oyvindh/Mustache.MSBuild/blob/main/LICENSE)
[![Build](https://github.com/oyvindh/Mustache.MSBuild/workflows/.NET%20Core/badge.svg?branch=main)](https://github.com/oyvindh/Mustache.MSBuild/actions)
[![Version](https://img.shields.io/nuget/v/Hic.Mustache.MSBuild.svg?color=royalblue)](https://www.nuget.org/packages/Mustache.MSBuild)
[![Downloads](https://img.shields.io/nuget/dt/Hic.Mustache.MSBuild.svg?color=green)](https://www.nuget.org/packages/Hic.Mustache.MSBuild)

This repository contains the code for MSBuild tasks that allow the expansion of Mustache templates during the build. Typically useful if you do any infrastructure as code with MSBuild or simply need to generate some files.

There are two main ways to run this. The most simple, is to use the `MustacheExpand` task. This takes a template and a data file as input and produce the expanded result in a file. The other way, `MustacheDirectoryExpand`, is to expand a directory structure where data can be overridden at every level. This mode becomes very useful is you are expanding configuration that does not support parameterization.

## MustacheExpand task

```xml
<MustacheExpand
    TemplateFile="mustache-templates/simple.mustache"
    DataFile="simple.json" />
```

## MustacheDirectoryExpand task

To use the directory expansion, you need to declare the directory input and output structure that you want.

```json
{
  "Children": [
    {
      "Name": "First"
    },
    {
      "Name": "Second",
      "Children": [
        {
            "Name": "FirstChild"
        }
      ]
    }
  ]
}
```

Then you can define parameters/data to the template expansion that override values downward in the tree structure

```txt
data
├── First
│   └── data.json
├── Second
│   └── FirstChild
│       └── data.json
└── data.json
```

Now, you can declare the task and do a directory wise template expansion.

```xml
<MustacheDirectoryExpand
    TemplateFile="app-resources.task.mustache"
    DataRootDirectory="data"
    DestinationRootDirectory="$(OutputPath)"
    DefaultDestinationFileName="expanded.txt"
    DefaultDataFileName="data.json"
    DirectoryStructureFile="directory-structure.json" />
```

## Contribute

Refer to the [contribution](CONTRIBUTE.md) docs.
