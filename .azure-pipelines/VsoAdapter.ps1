param(
    [Parameter(Mandatory=$true, ParameterSetName="FixBrokenHardlinks")]
    [String]
    $HardlinksEncoded,

    [Parameter(Mandatory=$true, ParameterSetName="PrepareTaskInputs")]
    [String]
    $BuildOutputsEncoded,
    
    [Parameter(Mandatory=$true, ParameterSetName="FixBrokenHardlinks")]
    [Parameter(Mandatory=$true, ParameterSetName="PrepareTaskInputs")]
    [ValidateScript({Test-Path $_ -PathType Container})]
    [System.IO.FileInfo]
    $SourceDir
)

function Set-VsoVariable {
    param(
        [Parameter(Mandatory=$true)]
        [String]
        $name,
    
        [Parameter(Mandatory=$true)]
        [String]
        $value
    )
    process {
        Write-Host "Setting vso variable $name=$value"
        Write-Host "##vso[task.setvariable variable=$name]$value"
    }
}

function Get-OtherHardlinks
{
    [OutputType([System.IO.FileInfo[]])]
    param(
        [ValidateScript({Test-Path $_ -PathType Leaf})]
        [System.IO.FileInfo]
        $path
    )
    process {
        find $SourceDir -samefile $path | Get-Item | ? { $_.FullName -ne $path.FullName }
    }
}

function Convert-ToMinimatch {
    [OutputType([String])]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline = $true)]
        [String[]]
        $values)
    process {
        $values -join "%0D%0A"
    }
}

function Convert-ToRelativePath {
    [OutputType([String])]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline = $true)]
        [System.IO.FileInfo]
        $path
    )
    process {
        $path.FullName.Substring($SourceDir.FullName.Length + 1)
    }
}

function Convert-FromHardlinkMap {
    [OutputType([String])]
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline = $true)]
        $brokenHardlinks
    )
    process {
        $($brokenHardlinks | % { "$(Convert-ToRelativePath $_.original)>$($($_.links | Convert-ToRelativePath | Split-Path) -join "<")" }) -join "|"
    }
}

function Convert-ToHardlinkMap {
    param(
        [Parameter(Mandatory=$true, ValueFromPipeline = $true)]
        [String]
        $encoded
    )
    process {
        foreach ($brokenLinkEncoded in $encoded -split "\|") {
            $tokens = $brokenLinkEncoded -split ">"
            @{
                original = Get-Item $(Join-Path $SourceDir $tokens[0])
                links = $tokens[1] -split "<" | % { Get-Item $(Join-Path $SourceDir $(Join-Path $_ $(Split-Path $tokens[0] -Leaf))) }
            }
        }
    }
}

function Test-OutputInDirectory {
    param(
        [Parameter(Mandatory=$true)]
        [System.IO.FileInfo]
        $outputFile,

        [Parameter(Mandatory=$true)]
        [AllowEmptyCollection()]
        [String[]]
        $testDirectories
    )
    process {
        $($testDirectories | ? { $(Convert-ToRelativePath $outputFile).StartsWith($_) }).Count -gt 0
    }
}

switch ($PsCmdlet.ParameterSetName) {
    "PrepareTaskInputs" {
        $buildOutputs = $BuildOutputsEncoded -split "\|" | Get-Item

        if ($null -ne $buildOutputs)
        {
            Set-VsoVariable "build.taskinput.sign" $(Convert-ToMinimatch $($buildOutputs | % { $_.FullName.Substring($SourceDir.FullName.Length) }))
        }        

        $brokenHardlinks = $buildOutputs | % { @{ original = $_; links = Get-OtherHardlinks $_ } } | ? { $_.links.Count -gt 0 }

        if ($null -ne $brokenHardlinks)
        {
            Set-VsoVariable "build.taskinput.brokenhardlinks" $(Convert-FromHardlinkMap $brokenHardlinks)
        }
    }

    "FixBrokenHardlinks" {
        $hardlinkMap = Convert-ToHardlinkMap $HardlinksEncoded
        foreach ($brokenLink in $hardlinkMap) {
            foreach ($link in $brokenLink.links) {
                Remove-Item $link
                ln $brokenLink.original $link
            }
        }
    }
}
