# Guide for releasing on Winget Repo

## Command to generate manifest files

Change the version and binary location. Note the x64 flag.

```
wingetcreate update Cognirush.MassImageCompressor --urls "https://github.com/Cognirush-Labs-LLP/icompress-win/releases/download/V4.2.0/MassImageCompressorInstallerV4_2_0.exe|x64" --version 4.2.0
```



