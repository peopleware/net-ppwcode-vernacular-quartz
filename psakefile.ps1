###############################################################################
###  PSAKE build script                                                     ###
###############################################################################
###  Copyright 2015 by PeopleWare n.v.                                      ###
###############################################################################
###  Authors: Ruben Vandeginste, Danny Van den Wouwer                       ###
###############################################################################
###                                                                         ###
###  A psake build file that configures                                     ###
###     - build and clean the solution                                      ###
###     - restore nuget packages                                            ###
###     - create and publish nuget packages                                 ###
###     - run unit tests                                                    ###
###     - generate documentation                                            ###
###                                                                         ###
###############################################################################


#region CONFIGURATION

###############################################################################
### CONFIGURATION                                                           ###
###############################################################################

###############################################################################
# Use build tools from .NET framework 4.8
#
Framework '4.8'

###############################################################################
# Properties that are used by this psake
# script; these can be overridden when
# invoking this script
#
Properties {
    # buildconfig: the configuration used for building
    # note that this also determines the deployment configuration (SlowCheetah)
    $buildconfig = 'Debug'
    $buildPlatformTarget = 'AnyCpu'

    # chatter: number to indicate verbosity during the execution of tasks
    #   0 - no output
    #   1 - minimal output, tasks executed
    #   2 - extra output, steps within tasks
    #   3 - even more output
    $chatter = 1
    # chattercolor: foreground color to use for task output
    $chattercolor = 'Green'

    # choco installer directives
    $chocoIncludeXmlFiles = $true
    $chocoIncludePdb = $true

    # used nunit version
    $nunitVersion = '3.12.0'
    $nUnitFramework = $null
}

#endregion


#region PRIVATE HELPERS

###############################################################################
### PRIVATE HELPERS                                                         ###
###############################################################################

###############################################################################
# Helper for printing out documentation
#
function PropertyDocumentation() {
    Write-Host 'Properties'
    Write-Host '----------'
    Write-Host "`$chatter                      The level of verbosity during task execution, defaults to 1."
    Write-Host  '                                0 - No output'
    Write-Host  '                                1 - Minimal output, tasks executed'
    Write-Host  '                                2 - Extra output, steps within tasks'
    Write-Host  '                                3 - Even more output, debug execution'
    Write-Host "`$chattercolor                 Foreground color to use for task output., defaults to Green"
    Write-Host "`$buildconfig                  Build configuration (Debug, Release), defaults to Debug."
    Write-Host "`$buildPlatformTarget          Build Target Platform (AnyCpu, x86, x64), defaults to AnyCpu."
    Write-Host "`$chocoIncludeXmlFiles        When creating a chocolatey-package, we can specify to include Xml-Files, defaults to $false."
    Write-Host "`$chocoIncludePdb              When creating a chocolatey-package, we can specify to include Pdb-Files, defaults to $false."
    Write-Host "`$nunitVersion                 Version of nunit-runner, defaults to 3.9.0"
    Write-Host "`$nUnitFramework               RuntimeFramework for nunit-runner, defaults to $null, If not specified, tests will run under the framework they are compiled with"
}

###############################################################################
# Helper for chatter
#
function Chatter {
    param
    (
        [string]
        $msg = '.',

        [int]
        $level = 3
    )

    if ($level -le $chatter) {
        Write-Host $msg -ForegroundColor $chattercolor
    }
}

###############################################################################
# Helper for to see if we run with Administrator privileges
#
function Test-Administrator
{
    $user = [Security.Principal.WindowsIdentity]::GetCurrent();
    (New-Object Security.Principal.WindowsPrincipal $user).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
}

###############################################################################
# Helper to determine our msbuild parameters
#
function Get-DefaultMsBuildParameters
{
    Param (
        [Parameter(Mandatory = $true)]
        [System.IO.FileInfo]$targetFile,

        [Parameter(Mandatory = $true)]
        [string[]]$tasks,

        [Parameter(Mandatory = $false)]
        [string[]]$configuration,

        [Parameter(Mandatory = $false)]
        [string[]]$platform,

        [Parameter(Mandatory = $false)]
        [string[]]$additionalParameters
    )

    if (-Not $configuration) {
        $configuration = $buildconfig
    }

    if (-Not $platform) {
        $platform = $buildPlatformTarget
        if ($platform -eq "AnyCpu") {
            $platform = "Any Cpu"
        }
    }

    $concatedTasks = ($tasks -join ';')
    $params = @(
        """$targetFile"""
        "/t:$concatedTasks"
        "/m"
        "/nr:false"
        "/nologo"
        "/p:Configuration=""$configuration"""
        "/p:Platform=""$platform"""
    )

    if ($chatter -eq 1) {
        $params += "/v:quiet"
    }
    elseif ($chatter -eq 2) {
        $params += "/v:minimal"
    }
    else {
        $params += "/v:normal"
    }

    if ($additionalParameters) {
        foreach($param in $additionalParameters) {
            $params += $param
        }
    }

    $params
}

###############################################################################
# Helper to invoke msbuild
#
function Invoke-MsBuild
{
    Param (
        [Parameter(Mandatory = $true)]
        [System.IO.FileInfo]$targetFile,

        [Parameter(Mandatory = $true)]
        [string[]]$tasks,

        [Parameter(Mandatory = $false)]
        [string[]]$configuration,

        [Parameter(Mandatory = $false)]
        [string[]]$platform,

        [Parameter(Mandatory = $false)]
        [string[]]$additionalParameters
    )

    $msbuildParams = Get-DefaultMsBuildParameters -targetFile $targetFile -tasks $tasks -configuration $configuration -platform $platform -additionalParameters $additionalParameters
    $message = "msbuild $tasks went wrong"

    Chatter "msbuild $msbuildParams" 2
    Exec { msbuild $msbuildParams } $message
}

###############################################################################
# Helper to determine our nuget parameters
#
function Get-DefaultNugetParameters
{
    Param (
        [Parameter(Mandatory = $true)]
        [string]$task,

        [Parameter(Mandatory = $true)]
        [string[]]$additionalParameters,

        [Parameter(Mandatory = $false)]
        [string]$configuration,

        [Parameter(Mandatory = $false)]
        [string[]]$properties
    )

    $params = @($task)
    if ($additionalParameters) {
        foreach($param in $additionalParameters) {
            $params += $param
        }
    }

    if ($chatter -eq 1) {
        $params += "-Verbosity quiet"
    }
    elseif ($chatter -eq 2) {
        $params += "-Verbosity normal"
    }
    else {
        $params += "-Verbosity detailed"
    }

    if ($configuration) {
        if (-Not $properties) {
            $properties = @()
        }

        $properties += "Configuration=$configuration"
    }

    if ($properties) {
        $joinedProperties = $properties -join ';'
        $params += "-Properties ""$joinedProperties"""
    }

    $params
}

###############################################################################
# Helper to invoke Nuget
# note: nuget commandline transparantly calls msbuild "restore" for new type csproj files
#
function Invoke-Nuget
{
    Param (
        [Parameter(Mandatory = $true)]
        [string]$task,

        [Parameter(Mandatory = $true)]
        [string[]]$additionalParameters,

        [Parameter(Mandatory = $false)]
        [string]$configuration,

        [Parameter(Mandatory = $false)]
        [string[]]$properties
    )

    $nugetParams = Get-DefaultNugetParameters -task $task -additionalParameters $additionalParameters -configuration $configuration -properties $properties
    $message = "nuget $task went wrong"

    Chatter "nuget $nugetParams" 2
    Exec { Invoke-Expression "nuget $nugetParams" } $message
}

#endregion

#region TASKS

###############################################################################
### TASKS                                                                   ###
###############################################################################

###############################################################################
# Default task, executed when no task is
# given explicitly
#
Task Default -depends ?

###############################################################################
# Help
#
Task ? -description 'Show help.' {
    Write-Host
    PropertyDocumentation
    WriteDocumentation
}

###############################################################################
# Helper to chat our properties
#
Task ShowProperties -description 'Show running properties' {
    Chatter "Properties used:" 1
    Chatter "  buildconfig=$buildconfig" 1
    Chatter "  buildPlatformTarget=$buildPlatformTarget" 1
    Chatter "  chocoIncludeXmlFiles=$chocoIncludeXmlFiles" 1
    Chatter "  chocoIncludePdb=$chocoIncludePdb" 1
    Chatter "  nunitVersion=$nunitVersion" 1
    Chatter "  nUnitFramework=$nUnitFramework" 1
}

###############################################################################
# Clean build artifacts and temporary files
#
Task Clean -description 'Clean build output.' -depends ShowProperties {

    Push-Location
    try
    {
        Chatter 'Cleaning solution' 2
        Set-Location 'src'
        $solution = Get-Item '*.sln' | Select-Object -First 1

        # msbuild clean
        Invoke-MsBuild -targetFile $solution.Name -tasks @('Clean')
        Set-Location '..'

        # clean up scratch folder
        Chatter 'Cleaning scratch' 2
        $path = "scratch"
        if (Test-Path $path) {
            Remove-Item -Path $path -Recurse -Force
        }

        # clean up bin/obj folders
        Chatter 'Cleaning bin/obj' 2
        Remove-Item -Path (Join-Path (Join-Path "src" "*") "bin"),(Join-Path (Join-Path "src" "*") "obj") -Recurse -Force
    }
    finally
    {
        Pop-Location
    }
}

###############################################################################
# Clean ReSharper cache folder
#
Task ReSharperClean -description 'Clean ReSharper cache folders.' -depends ShowProperties {

    Push-Location
    try
    {
        # clean up cache folder
        $path = Join-Path 'src' '_ReSharper.Caches'
        if (Test-Path $path) {
            Remove-Item -Path $path -Recurse -Force
        }
    }
    finally
    {
        Pop-Location
    }
}

###############################################################################
# Clean packages folder
#
# Depends on ReSharperClean to prevent cached references to removed packages.
#
Task PackageClean -description 'Clean NuGet packages folder.' -depends ReSharperClean {

    Push-Location
    try
    {
        # clean up packages folder
        $path = Join-Path 'src' 'packages'
        if (Test-Path $path) {
            Remove-Item -Path $path -Recurse -Force
        }
    }
    finally
    {
        Pop-Location
    }
}

###############################################################################
# Full clean
#
Task FullClean -description 'Clean build output, generated packages and NuGet packages folder.' -depends Clean,PackageClean

###############################################################################
# Restore packages
#
Task PackageRestore -description 'Restore NuGet package dependencies.' -depends PackageClean {

    Push-Location
    try
    {
        Set-Location 'src'
        $solution = Get-Item '*.sln' | Select-Object -First 1

        # traditional nuget command-line restore
        # note, nuget commandline transparantly calls msbuild "restore" for new type csproj files
        $nugetRestoreParams = @(
            """$solution"""
            "-NonInteractive"
        )
        Invoke-Nuget -task restore -additionalParameters $nugetRestoreParams
    }
    finally
    {
        Pop-Location
    }
}

###############################################################################
# Restore
#
# Depends on PackageRestore to restore packages, and Clean to put the solution
# in a clean state so VS2017 is willing to open it without complaints.
#
Task Restore -description 'Restore solution.' -depends PackageRestore,Clean {
}

###############################################################################
# Build the solution
#
# Depends on Clean to ensure a clean build, not using any left-over compile artifacts.
#
Task Build -description 'Build the solution.' -depends Clean {

    Push-Location
    try
    {
        Set-Location 'src'
        $solution = Get-Item '*.sln' | Select-Object -First 1
        Invoke-MsBuild -targetFile $solution.Name -tasks @('Restore', 'Build')
    }
    finally
    {
        Pop-Location
    }
}

###############################################################################
# Full build
#
Task FullBuild -description 'Do a full build starting from a clean solution.' -depends Restore,Build

###############################################################################
# Create a NuGet package, based on new project VS2017 style
#
# Depends on Clean to ensure a clean build, not using any left-over compile artifacts.
#
Task Pack2017 -description 'Create a nuget-package (new project 2017 style).' -depends FullClean {

    Push-Location
    try
    {
        Push-Location 'src'
        try {
            $solution = Get-Item '*.sln' | Select-Object -First 1
            if ($solution) {
                $additionalParameters = @(
                    "/p:IncludeSymbols=True"
                    "/p:IncludeSource=True"
                )
                Invoke-MsBuild -targetFile $solution.Name -tasks @('Restore','Pack') -additionalParameters $additionalParameters
            }
        }
        finally {
            Pop-Location
        }
    }
    finally
    {
        Pop-Location
    }
}

###############################################################################
# Create a NuGet package (legacy project style)
#
# Depends on Clean to ensure a clean build, not using any left-over compile artifacts.
#
Task Pack -description 'Create a nuget-package (legacy project style).' -depends FullBuild {

    Push-Location
    try
    {
        $scratchDirectory = Get-Item -Path 'scratch'

        Push-Location 'src'
        try {
            Chatter '  Searching nuspec files ...' 1
            Get-ChildItem '*.nuspec' -Recurse | ForEach-Object {
                $nuspecFile = $_

                Chatter "  Found Searching $($nuspecFile.FullName)" 1

                $csprojFile = Get-Item (Join-Path -Path $nuspecFile.Directory.FullName -ChildPath "$($nuspecFile.BaseName).csproj")
                if ($csprojFile.Exists) {
                    [System.IO.DirectoryInfo]$outputDirectory = Join-Path -Path $scratchDirectory.FullName -ChildPath 'nuget'

                    if (-Not $outputDirectory.Exists) {
                        $outputDirectory = New-Item -Path $outputDirectory.FullName -ItemType Directory
                    }

                    $nugetPackParams = @(
                        """$($csprojFile.FullName)"""
                        "-OutputDirectory ""$($outputDirectory.FullName)"""
                    )
                    Invoke-Nuget -task pack -additionalParameters $nugetPackParams -configuration $buildconfig
                }
            }
        }
        finally {
            Pop-Location
        }
    }
    finally
    {
        Pop-Location
    }
}

###############################################################################
# Run unit tests
#
Task Test -description 'Run the unit tests using NUnit console test runner.' -depends FullBuild {

    Push-Location
    try
    {
        $nunitApplicationName = 'nunit3-console.exe'
        $nunitNugetPackageName = 'NUnit.ConsoleRunner'
        $nunitNugetVersionedPackageName = "$nunitNugetPackageName.$($nunitVersion)"
        $packageNUnitPath = `
            Join-Path -Path 'src' -ChildPath 'packages' | `
            Join-Path -ChildPath $nunitNugetVersionedPackageName

        # Can we find nunit-runner in our traditional packages directory?
        $runner = $null
        $runnerPath = `
            Join-Path -Path $packageNUnitPath -ChildPath 'tools' | `
            Join-Path -ChildPath $nunitApplicationName
        if (Test-Path $runnerPath) {
            $runner = Get-Item -Path $runnerPath
        }

        # If we didn't found it, install nunit-runner in our scratch directory
        if ($null -eq $runner) {
            $scratchNUnitPath = Join-Path -Path '.\scratch' -ChildPath 'nunit'

            $nugetRestoreParams = @(
                """$nunitNugetPackageName"""
                "-Version '$($nunitVersion)'"
                "-OutputDirectory ""$scratchNUnitPath"""
                "-NonInteractive"
            )
            Invoke-Nuget -task install -additionalParameters $nugetRestoreParams

            $runnerPath = `
                Join-Path -Path $scratchNUnitPath -ChildPath $nunitNugetVersionedPackageName | `
                Join-Path -ChildPath 'tools' | `
                Join-Path -ChildPath $nunitApplicationName
            if (Test-Path $runnerPath) {
                $runner = Get-Item $runnerPath
            }
        }

        # stop if we don't have a nunit-runner
        if ($null -eq $runner)
        {
            throw 'Cannot find or fetch nunit runner.'
        }

        Chatter "Found nunit runner: $runner" 2

        # find unit test dlls
        Chatter 'Searching test projects ...' 1
        Get-ChildItem -Path (Join-Path -Path 'scratch' -ChildPath 'bin') -Filter *Tests.dll -Recurse | ForEach-Object {
            $testdll = $_
            $testBaseName = $testdll.BaseName

            # generate folder for test output
            Chatter "  Found test project: $testBaseName" 1

            $nunitFolder = New-Item -ItemType Directory -Path (Join-Path -Path "scratch" -ChildPath "nunit") -Force

            # intialise runner args
            $testrunnerargs = @(
                "--work:$nunitFolder"
                "--result:nunit-test-results-$testBaseName.xml"
                "--out:nunit-stdout-$testBaseName.txt"
                '--labels:all'
            )

            if ($null -ne $nUnitFramework) {
                $testrunnerargs += "--framework:$nUnitFramework"
            }

            # execute runner
            Chatter "$($runner.FullName) $testdll.FullName $testrunnerargs" 2
            Exec { & "$($runner.FullName)" $_.FullName $testrunnerargs } "Running nunit runner went wrong"
        }
    }
    finally
    {
        Pop-Location
    }
}

###############################################################################
# WixInstaller, build each wixproj in our src directory
#
Task WixInstaller -description 'Do a full build starting from a clean solution and create all WixInstaller(s)' -depends FullBuild {

    Push-Location
    try
    {
        Get-ChildItem -Path 'src' -Filter '*.wixproj' -Recurse | ForEach-Object {
            Invoke-MsBuild -targetFile $_.FullName -tasks @('Build')
        }
    }
    finally {
        Pop-Location
    }
}

###############################################################################
# Open, starting all *.sln files, inside .\src directory
#
Task Open -description 'starting all *.sln files, inside .\src directory' -depends Restore {

    Push-Location
    try
    {
        Get-ChildItem -Path 'src' -Filter '*.sln' -Recurse | ForEach-Object {
            $solution = $_.FullName
            Chatter "start $solution" 2
            Invoke-Item $solution
        }
    }
    finally {
        Pop-Location
    }
}

###############################################################################
# ChocoInstaller, build our choco-installer (portable version)
#
Task ChocoInstaller `
    -description 'Do a full build starting from a clean solution and create our portable chocolatey-package' `
    -depends FullBuild {

    Push-Location
    try
    {
        if ($env:ChocolateyInstall) {
            # There should be 2 folder(s)
            #  - choco-scripts, contains specific powershell chocolatey files (install, uninstall, ...)
            #  - choco-nuspecs, contains all nupsec to build
            if ((Test-Path -Path 'choco-scripts') -and (Test-Path -Path 'choco-nuspecs')) {
                $chocoScripts = Get-item 'choco-scripts'
                $chocoNuSpecs = Get-Item 'choco-nuspecs'
                $chocoScratch = Join-Path -Path 'scratch' -ChildPath 'choco'
                $chocoDestination = New-Item -ItemType Directory -Path $chocoScratch -Force

                # copy content of our 2 choco folders to our scratch choco folder
                Copy-item -Force "$($chocoScripts.FullName)\*" -Destination $chocoDestination.FullName -Exclude '.git'
                Copy-item -Force "$($chocoNuSpecs.FullName)\*" -Destination $chocoDestination.FullName -Exclude '.git'

                #search for a nuspec file
                Get-ChildItem -Path $chocoDestination.FullName -Filter *.nuspec | ForEach-Object {
                    $nuspecFile = $_
                    Chatter "  Processing $($nuspecFile.Name)" 1

                    # make a difference between Cli and website (by convention IIS)
                    if ($nuspecFile.Name -match 'IIS') {
                        Chatter "    Detected a web project, let's publish if we have the publish profile" 1
                        $projectFolder = Join-path -Path 'src' -ChildPath $nuspecFile.BaseName

                        if (Test-Path -Path $projectFolder) {
                            $projectFolder = Get-Item -Path $projectFolder

                            $projectFile =
                                Get-ChildItem $projectFolder.FullName -Recurse -Filter *$($nuspecFile.BaseName).csproj `
                                    | Select-Object -First 1
                            $publishProfile =
                                Join-Path -Path $projectFolder.FullName -ChildPath 'Properties' | `
                                Join-Path -ChildPath 'PublishProfiles' | `
                                Join-Path -ChildPath 'Publish-Scratch.pubxml'

                            if ($projectFile -and $publishProfile) {
                                $additionalParameters = @(
                                    "/p:DeployOnBuild=True"
                                    "/p:PublishProfile=""$publishProfile"""
                                )
                                Invoke-MsBuild -targetFile $projectFile.FullName -tasks @('Build') -platform 'AnyCPU' -additionalParameters $additionalParameters
                            }
                        }
                    }

                    # search for a folder, simular to our nuspec file
                    $packageFolder= "$($nuspecFile.BaseName)-$buildconfig-$buildPlatformTarget"
                    Chatter "  Searching for PackageFolder $packageFolder" 1
                    $scratchBin = Join-Path -Path 'scratch' -ChildPath 'bin'
                    $packageFolder = `
                        Get-ChildItem $scratchBin.FullName -Recurse `
                            | Where-Object {$_.PSIsContainer -eq $true -and $_.Name -match $packageFolder} `
                            | Select-Object -First 1
                    if ($packageFolder) {
                        # search for a executable file, simular to our nuspec file
                        Chatter "  Searching for *$($nuspecFile.BaseName).exe inside PackageFolder" 1
                        $targetFileName = `
                            Get-ChildItem $($packageFolder.FullName) -Recurse -Filter *$($nuspecFile.BaseName).exe `
                                | Select-Object -First 1
                        if (-Not $targetFileName) {
                            Chatter "  Searching for *$($nuspecFile.BaseName).dll inside PackageFolder" 1
                            $targetFileName = `
                                Get-ChildItem $($packageFolder.FullName) -Recurse -Filter *$($nuspecFile.BaseName).dll `
                                    | Select-Object -First 1
                        }

                        if ($targetFileName) {
                            $targetVersionInfo = (Get-Item $targetFileName.FullName).VersionInfo
                            $id = $targetVersionInfo.FileDescription
                            $zipProg = Join-Path (Join-Path $env:ChocolateyInstall "tools") "7z.exe"
                            $zipPackage = (Join-Path -Path $chocoDestination.FullName -ChildPath $id) + ".7z"

                            $zipProgParams = @(
                                "a '$zipPackage'"
                                "$($packageFolder.FullName)\*"
                                "-t7z -r -m0=BCJ2 -m1=LZMA2:d=1024m -aoa"
                            )

                            if ($chocoIncludeXmlFiles -eq $false) {
                                $zipProgParams += "'-x!*.xml'"
                            }

                            if ($chocoIncludePdb -eq $false) {
                                $zipProgParams += "'-x!*.pdb'"
                            }

                            Chatter "$zipProg $zipProgParams" 2
                            Exec { Invoke-Expression "$zipProg $zipProgParams" } "7z not available or something went wrong"

                            Push-Location $chocoDestination.FullName
                            try {
                                $version = $targetVersionInfo.ProductVersion
                                $description = $targetVersionInfo.Comments
                                $author = $targetVersionInfo.CompanyName
                                $title = $targetVersionInfo.ProductName
                                $copyright = $targetVersionInfo.LegalCopyright
                                $zipPackage = $id

                                $chocoPackParams = @(
                                    "'$($nuspecFile.Name)'"
                                    "--version '$version'"
                                    "id='$id'"
                                    "description='$description'"
                                    "author='$author'"
                                    "title='$title'"
                                    "copyright='$copyright'"
                                    "zipPackage='$zipPackage'"
                                );
                                Chatter "choco pack $chocoPackParams" 2
                                Exec { Invoke-Expression "choco pack $chocoPackParams" } "choco pack went wrong"
                            }
                            finally {
                                Pop-Location
                            }
                        }
                    }
                }
            }
        }
    }
    finally {
        Pop-Location
    }
}

###############################################################################
# ChocoInstall, build our choco-installer (portable version) and install it
#
Task ChocoInstall `
    -description 'Do a full build starting from a clean solution and create our portable chocolatey-package and install it (Requires Administrator rights)' `
    -depends ChocoInstaller {

    if (Test-Administrator) {
        Push-Location
        try
        {
            # choco should be installed
            if ($env:ChocolateyInstall) {
                $chocoDestination = Get-Item -Path (Join-Path "scratch" "choco")
                Chatter $chocoDestination 2
                Get-ChildItem $chocoDestination.FullName -Filter *.nuspec | ForEach-Object {
                    $nuspecFile = $_
                    Chatter "  Searching for *$($nuspecFile.BaseName).exe inside PackageFolder" 1
                    $targetFileName = `
                        Get-ChildItem $($packageFolder.FullName) -Recurse -Filter *$($nuspecFile.BaseName).exe `
                            | Select-Object -First 1
                    if (-Not $targetFileName) {
                        Chatter "  Searching for *$($nuspecFile.BaseName).dll inside PackageFolder" 1
                        $targetFileName = `
                        Get-ChildItem $($packageFolder.FullName) -Recurse -Filter *$($nuspecFile.BaseName).dll `
                            | Select-Object -First 1
                    }

                    if ($targetFileName) {
                        $targetVersionInfo = (Get-Item $targetFileName.FullName).VersionInfo
                        $id = $targetVersionInfo.FileDescription
                        $version = $targetVersionInfo.ProductVersion
                        $chocoInstallParams = @(
                            "'$id'"
                            "--version $version"
                            "--source '$($chocoDestination.FullName)'"
                            "-yes"
                            "--side-by-side"
                            "--params '/CONFIG:$buildConfig'"
                        )
                        Chatter "choco install $chocoInstallParams" 2
                        Exec { Invoke-Expression "choco install $chocoInstallParams" } "choco install went wrong"
                    }
                }
            }
        }
        finally {
            Pop-Location
        }
    }
    else {
        Chatter "This taks is only available when running asn an administrator." 1
    }
}

###############################################################################
# Installers, build our choco-installer / dsc-installer / wix-installer / nuget packs
#
Task Installers `
    -description 'Do a full build starting from a clean solution and create following packages if available: choco, dsc, wix, nuget' `
    -depends ChocoInstaller,WixInstaller,Pack

###############################################################################
# Installers, build our choco-installer / dsc-installer / wix-installer / nuget packs
#
Task Installers2017 `
    -description 'Do a full build starting from a clean solution and create following packages if available: choco, dsc, wix, nuget' `
    -depends ChocoInstaller,WixInstaller,Pack2017

#endregion
