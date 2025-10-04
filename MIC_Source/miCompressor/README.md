This is common guide to package and release Mass Image Compressor. 

# Creating Unpackaged Version
Make these changes in `miCompressor.csproj` files
- Set `EnableMsixTooling` to `false`
- Set `WindowsPackageType` to `None`
- Set `GenerateAppxPackageOnBuild` to `false`
- Set `CodeConsts.cs` > `IsForStoreDistribution` to `false`

# Creating MSIX with Debug Symbol (for Microsoft Store)
Make these changes in `miCompressor.csproj` files
- Set `EnableMsixTooling` to `true`
- Set `WindowsPackageType` to `MSIX`
- Set `GenerateAppxPackageOnBuild` to `true`
- Set `CodeConsts.cs` > `IsForStoreDistribution` to `true`

Ensure you have valid publishing certificate (for store). Check below items:
 - `miCompressor.csproj` > `PackageCertificateThumbprint` 
 - `Package.appxmanifest` > `Publisher` , `Version`, If you are not Cognirush Labs, you will need change `Name` also.

# Version Upgrade
**General rules for version increase** - Increase Major version if code is rewritten. Increase minor version if you want user to update the software (shows notification in software). Increase patch version if you want to make a new release but do not want to in-software notification to prompt user to update the software. 

Unfortunately, version number is embedded at many places in the code. Update these files to have latest version before releasing. 

- These can be found if you search in Visual Studio by current major and minor version number (e.g. search '4.1' if current version is 4.1)
  - `CodeConsts.cs` > `Version`
  - `miCompressor.csproj` > `PropertyGroup` > `Version`
  - `Package.appxmanifest` > `Identity[Version]`



# Creating your own distribution

Modifying software source for personal/closed group release: Make sure source is publicly available (AGPL license terms) and binaries are NOT publicly available. 

Below rules apply for public release of binaries.

## Basic Rules
- Change the name of application to something like `Mass Image Compressor (<YOUR PORT NAME>)` if it is a public release. 
- If your changes aren't major rewrite, update only minor version.
- Modify VersionChecker.cs > VersionUrl to something yours, or just keep VersionUrl an empty string. 

You are free to distribute without any changes mentioned below if you are redistributing binaries created by Cognirush Labs LLP. But if you have built the binary yourselves, you must follow below practice to disassociate your distribution from Cognirush Labs LLP. 

## Do not change original license
You are bound by AGPL license with which this software (original)  is distributed. Any redistribution must carry the same license.

## Disclosure 
Please make sure you do not imply it in anyway affiliated with Cognirush Labs LLP. So please put below notice in your software description. 

```
This is an independent fork of Mass Image Compressor licensed under the GNU AGPL.
Source code (this version): `<your source-code link>`· 
Not affiliated with or endorsed by the original authors i.e. Cognirush Labs LLP.
```

## Removing Cognirush Support Channels
We are a small team with limited resources. **To avoid confusion for everyone, please handle support request for your custom distribution yourself**. Here is general guidance:  

1. Simplest way to do this, is, find all `HyperlinkButton` in the xaml files and either remove it, or change it to point to your support channels. 
2. Modify references to Cognirush Labs in `csproj` file, such as `Authors`, `PackageProjectUrl`, `RepositoryUrl`
3. Modify Copyright in `csproj` file to add your own copyright (as a fork maintainer), without removing original copyright of Cognirush Labs LLP.

Again, nothing to be paranoid about redistributing the modified version of Mass Image Compressor. If you are confused, contact us 🙂 We will help you.

