<#
Launches Mass Image Compressor, runs the given steps through UI Automation, then kills it.
The user's saved settings are backed up first and restored afterwards, because Compress overwrites them.
Run with Windows PowerShell 5.1 (powershell.exe): UI Automation comes from .NET Framework.

  powershell -NoProfile -ExecutionPolicy Bypass -File driver.ps1 [options] <step> <step> ...

Steps are listed in SKILL.md. A failed step stops the run and exits with code 1.
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [string]$Exe,
    [string]$ImagePath,
    [string]$OutDir = (Join-Path $env:TEMP 'mic-driver'),
    [int]$SavedFormat = -1,
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$Steps
)
$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
if (-not $Exe) { $Exe = Join-Path $repo 'MIC_Source\miCompressor\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\Mass Image Compressor.exe' }
if (-not (Test-Path $Exe)) { Write-Output "App not built: $Exe"; exit 1 }

$shots = Join-Path $OutDir 'shots'
New-Item -ItemType Directory -Force $shots | Out-Null
if (-not $ImagePath) {
    # A copy outside the repo: the app's default output folder sits next to the input.
    $inputDir = Join-Path $OutDir 'input'
    if (Test-Path $inputDir) { Remove-Item $inputDir -Recurse -Force }
    New-Item -ItemType Directory $inputDir | Out-Null
    $ImagePath = Join-Path $inputDir 'sample.png'
    Copy-Item (Join-Path $repo 'MIC_Source\miCompressor\Assets\SplashScreen.scale-400.png') $ImagePath
}

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System; using System.Drawing; using System.Runtime.InteropServices; using System.Threading;
public static class MicNative {
  [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }
  [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
  [DllImport("user32.dll")] static extern void keybd_event(byte k, byte s, uint f, UIntPtr e);
  [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
  public static bool IsForeground(IntPtr h) { return GetForegroundWindow() == h; }
  public static void PrintShot(IntPtr h, string path) {
    RECT r; GetWindowRect(h, out r);
    using (var b = new Bitmap(r.R - r.L, r.B - r.T)) using (var g = Graphics.FromImage(b)) {
      IntPtr dc = g.GetHdc(); PrintWindow(h, dc, 2); g.ReleaseHdc(dc); b.Save(path);
    }
  }
  public static void ScreenShot(IntPtr h, string path) {
    SetForegroundWindow(h); Thread.Sleep(400);
    RECT r; GetWindowRect(h, out r);
    using (var b = new Bitmap(r.R - r.L, r.B - r.T)) using (var g = Graphics.FromImage(b)) {
      g.CopyFromScreen(r.L, r.T, 0, 0, b.Size); b.Save(path);
    }
  }
  public static void RightClick(int x, int y) { SetCursorPos(x, y); Thread.Sleep(300); mouse_event(8, 0, 0, 0, UIntPtr.Zero); mouse_event(16, 0, 0, 0, UIntPtr.Zero); }
  public static void Key(byte k) { keybd_event(k, 0, 0, UIntPtr.Zero); keybd_event(k, 0, 2, UIntPtr.Zero); }
  public static void Esc() { Key(0x1B); }
}
'@

$UI = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$CT = [System.Windows.Automation.ControlType]
$ExpandP = [System.Windows.Automation.ExpandCollapsePattern]::Pattern
$InvokeP = [System.Windows.Automation.InvokePattern]::Pattern
$SelectP = [System.Windows.Automation.SelectionPattern]::Pattern
$SelectItemP = [System.Windows.Automation.SelectionItemPattern]::Pattern
$ValueP = [System.Windows.Automation.ValuePattern]::Pattern
$ToggleP = [System.Windows.Automation.TogglePattern]::Pattern

function Cond($prop, $value) { [System.Windows.Automation.PropertyCondition]::new($prop, $value) }
function ById($id) { $w.FindFirst($TS::Descendants, (Cond $UI::AutomationIdProperty $id)) }
function NeedById($id) { $e = ById $id; if (-not $e) { throw "No element with id '$id'" }; $e }
function AllByName($name) { @($w.FindAll($TS::Descendants, (Cond $UI::NameProperty $name)) | Where-Object { -not $_.Current.IsOffscreen }) }
function IsShown($name) { (AllByName $name).Count -gt 0 }
function Label($item) {
    $t = $item.FindFirst($TS::Descendants, (Cond $UI::ControlTypeProperty $CT::Text))
    if ($t -and $t.Current.Name) { $t.Current.Name } else { $item.Current.Name }
}
function FormatItems() {
    # The compression list is also made of list items, named after their class.
    @($w.FindAll($TS::Descendants, (Cond $UI::ControlTypeProperty $CT::ListItem)) | Where-Object { $_.Current.Name -notlike 'miCompressor.*' })
}
function CurrentFormat() {
    $s = (NeedById 'OutputFormatCombobox').GetCurrentPattern($SelectP).Current.GetSelection()
    if ($s.Count -gt 0) { Label $s[0] } else { '(none)' }
}
function MenuItems() {
    # Flyouts can appear under the window or as their own top-level popup.
    $names = [ordered]@{}
    foreach ($scope in @($w, $UI::RootElement)) {
        foreach ($e in $scope.FindAll($TS::Descendants, (Cond $UI::ControlTypeProperty $CT::MenuItem))) {
            try { if ($e.Current.ProcessId -eq $app.Id -and -not $e.Current.IsOffscreen) { $names[$e.Current.Name] = $e } } catch { }
        }
    }
    $names
}

function Run-Step($step) {
    $parts = $step -split ':', 2
    $name = $parts[0]
    $arg = if ($parts.Count -gt 1) { $parts[1] } else { '' }
    Write-Output "== $step"
    switch ($name) {
        'state' {
            $s = [ordered]@{ format = CurrentFormat; previewOpen = -not (IsShown 'COMPRESSION LIST'); previewLinks = (AllByName 'Preview').Count }
            foreach ($t in 'QUALITY', 'RESIZE', 'Metadata', 'Keep Created/Modified Time', 'Apply Watermark', 'RENAME OUTPUT IMAGE') { $s[$t] = IsShown $t }
            ($s.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ' | '
        }
        'tree' {
            $wanted = 'ControlType.ComboBox', 'ControlType.Button', 'ControlType.CheckBox', 'ControlType.Text', 'ControlType.Slider', 'ControlType.ListItem', 'ControlType.TreeItem', 'ControlType.MenuItem', 'ControlType.SplitButton', 'ControlType.Hyperlink', 'ControlType.Edit'
            foreach ($e in $w.FindAll($TS::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
                try {
                    $c = $e.Current
                    if ($c.IsOffscreen -or $wanted -notcontains $c.ControlType.ProgrammaticName) { continue }
                    if ($c.Name -or $c.AutomationId) { "{0,-12} '{1}' id='{2}' enabled={3}" -f $c.ControlType.ProgrammaticName.Replace('ControlType.', ''), $c.Name, $c.AutomationId, $c.IsEnabled }
                } catch { }
            }
        }
        'formats' {
            $combo = NeedById 'OutputFormatCombobox'
            $combo.GetCurrentPattern($ExpandP).Expand(); Start-Sleep -Milliseconds 800
            # UI Automation reports the popup's items twice.
            (FormatItems | ForEach-Object { Label $_ } | Select-Object -Unique) -join ' | '
            $combo.GetCurrentPattern($ExpandP).Collapse(); Start-Sleep -Milliseconds 600
        }
        'format' {
            $combo = NeedById 'OutputFormatCombobox'
            if (-not $combo.Current.IsEnabled) { throw 'Format dropdown is disabled (Replace Original is selected)' }
            $combo.GetCurrentPattern($ExpandP).Expand(); Start-Sleep -Milliseconds 800
            $item = FormatItems | Where-Object { (Label $_) -eq $arg } | Select-Object -First 1
            if (-not $item) { throw "No format named '$arg'" }
            $item.GetCurrentPattern($SelectItemP).Select(); Start-Sleep -Milliseconds 500
            try { $combo.GetCurrentPattern($ExpandP).Collapse() } catch { }
            Start-Sleep -Milliseconds 900
            "format is now $(CurrentFormat)"
        }
        'location' {
            (NeedById 'OutputLocationSettingsDropDownButton').GetCurrentPattern($ExpandP).Expand(); Start-Sleep -Milliseconds 800
            $items = MenuItems
            if (-not $items.Contains($arg)) { throw "No location named '$arg'. Found: $(@($items.Keys) -join ' | ')" }
            $items[$arg].GetCurrentPattern($InvokeP).Invoke(); Start-Sleep -Seconds 1
            "format dropdown enabled=$((NeedById 'OutputFormatCombobox').Current.IsEnabled), format=$(CurrentFormat)"
        }
        'folder' {
            $box = NeedById 'FoderPathTextBox' # the id's spelling is what the app uses
            if ($arg) { $box.GetCurrentPattern($ValueP).SetValue($arg); Start-Sleep -Seconds 1 }
            "output folder=$($box.GetCurrentPattern($ValueP).Current.Value)"
        }
        'expand' {
            foreach ($e in $w.FindAll($TS::Descendants, (Cond $UI::ControlTypeProperty $CT::TreeItem))) { try { $e.GetCurrentPattern($ExpandP).Expand() } catch { } }
            Start-Sleep -Seconds 1
            "preview links=$((AllByName 'Preview').Count)"
        }
        'preview' {
            $links = AllByName 'Preview'
            if ($links.Count -eq 0) { throw 'No visible Preview link' }
            $links[0].GetCurrentPattern($InvokeP).Invoke(); Start-Sleep -Seconds 2
            "preview open=$(-not (IsShown 'COMPRESSION LIST'))"
        }
        'edit' {
            # The file row's link. A folder row has none, and Preview's toolbar button is reached with click:PreviewEditButton.
            $links = AllByName 'Edit'
            if ($links.Count -eq 0) { throw 'No visible Edit link' }
            $links[0].GetCurrentPattern($InvokeP).Invoke(); Start-Sleep -Seconds 2
            "edit open=$((ById 'EditDone') -ne $null)"
        }
        'click' {
            # Through UI Automation, so the user's mouse and other windows aren't involved. Takes an automation id or a visible name.
            $e = ById $arg
            if (-not $e) { $e = @(AllByName $arg) | Select-Object -First 1 }
            if (-not $e) { throw "No control with id or name '$arg'" }
            $pattern = $null
            if ($e.TryGetCurrentPattern($InvokeP, [ref]$pattern)) { $pattern.Invoke() }
            elseif ($e.TryGetCurrentPattern($ToggleP, [ref]$pattern)) { $pattern.Toggle() }
            elseif ($e.TryGetCurrentPattern($SelectItemP, [ref]$pattern)) { $pattern.Select() }
            else { throw "'$arg' can't be clicked through UI Automation" }
            Start-Sleep -Seconds 1
            "clicked $arg"
        }
        'key' {
            # A key goes to whichever window is in front, so it's only sent if that's this app.
            $codes = @{ Left = 0x25; Up = 0x26; Right = 0x27; Down = 0x28; Enter = 0x0D; Esc = 0x1B }
            if (-not $codes.ContainsKey($arg)) { throw "Unknown key '$arg'. Known: $($codes.Keys -join ', ')" }
            [void][MicNative]::SetForegroundWindow($hwnd); Start-Sleep -Milliseconds 400
            if (-not [MicNative]::IsForeground($hwnd)) { throw 'The app is not the foreground window, so no key was sent' }
            [MicNative]::Key([byte]$codes[$arg]); Start-Sleep -Milliseconds 800
            "pressed $arg"
        }
        'resize' {
            # resize:<width>,<height> of the window.
            $size = $arg -split ','
            $w.GetCurrentPattern([System.Windows.Automation.TransformPattern]::Pattern).Resize([double]$size[0], [double]$size[1]); Start-Sleep -Seconds 1
            "resized window to $arg"
        }
        'text' {
            "$arg = '$((NeedById $arg).Current.Name)'"
        }
        'add' {
            (NeedById 'InputPathTextBox').GetCurrentPattern($ValueP).SetValue($arg); Start-Sleep -Milliseconds 600
            (NeedById 'AddInputPathButton').GetCurrentPattern($InvokeP).Invoke(); Start-Sleep -Seconds 2
            "added $arg"
        }
        'type' {
            # type:<automation id>:<text>, leaving the box focused so that key:Enter goes to it.
            $idAndText = $arg -split ':', 2
            $box = NeedById $idAndText[0]
            $box.GetCurrentPattern($ValueP).SetValue($idAndText[1]); $box.SetFocus(); Start-Sleep -Milliseconds 500
            "typed '$($idAndText[1])' in $($idAndText[0])"
        }
        'slider' {
            # slider:<automation id>:<value>
            $idAndValue = $arg -split ':', 2
            (NeedById $idAndValue[0]).GetCurrentPattern([System.Windows.Automation.RangeValuePattern]::Pattern).SetValue([double]$idAndValue[1]); Start-Sleep -Milliseconds 600
            "set $($idAndValue[0]) to $($idAndValue[1])"
        }
        'value' {
            "$arg = '$((NeedById $arg).GetCurrentPattern($ValueP).Current.Value)'"
        }
        'config' {
            $file = Join-Path $env:LOCALAPPDATA "miCompressor\user_config\$arg.json"
            if (Test-Path $file) { "$arg = $([IO.File]::ReadAllText($file))" } else { "$arg is not saved" }
        }
        'shown' {
            "$arg shown=$((ById $arg) -ne $null)"
        }
        'rect' {
            "$arg at $((NeedById $arg).Current.BoundingRectangle)"
        }
        'size' {
            $image = [System.Drawing.Image]::FromFile($arg)
            try { "$arg = $($image.Width) x $($image.Height)" } finally { $image.Dispose() }
        }
        'menu' {
            # The last match is the deepest row: the file, not the list header or the folder row.
            $rows = @($w.FindAll($TS::Descendants, (Cond $UI::NameProperty ([IO.Path]::GetFileName($ImagePath)))))
            if ($rows.Count -eq 0) { throw 'No row for the input image' }
            [void][MicNative]::SetForegroundWindow($hwnd); Start-Sleep -Milliseconds 400
            $r = $rows[$rows.Count - 1].Current.BoundingRectangle
            [MicNative]::RightClick([int]($r.X + $r.Width / 2), [int]($r.Y + $r.Height / 2)); Start-Sleep -Seconds 1
            "right-click menu: $(@((MenuItems).Keys) -join ' | ')"
            if ($arg) { [MicNative]::ScreenShot($hwnd, (Join-Path $shots "$arg.png")); "screenshot: $(Join-Path $shots "$arg.png")" }
            [MicNative]::Esc(); Start-Sleep -Milliseconds 600
        }
        'compress' {
            # The folder box leaves the screen as soon as compression starts.
            $folder = (NeedById 'FoderPathTextBox').GetCurrentPattern($ValueP).Current.Value
            (NeedById 'CompressAllButton').GetCurrentPattern($InvokeP).Invoke()
            "compressing, output folder: $folder"
        }
        'waitfile' {
            $deadline = (Get-Date).AddSeconds(60)
            while (-not (Test-Path $arg)) {
                if ((Get-Date) -gt $deadline) { throw "File did not appear within 60s: $arg" }
                Start-Sleep -Milliseconds 500
            }
            Start-Sleep -Seconds 3 # the file appears before the tools and the results screen finish
            "$arg = $((Get-Item $arg).Length) bytes"
        }
        'shot' { $p = Join-Path $shots "$arg.png"; [MicNative]::PrintShot($hwnd, $p); "screenshot: $p" }
        'screen' { $p = Join-Path $shots "$arg.png"; [MicNative]::ScreenShot($hwnd, $p); "screenshot: $p" }
        'sleep' { Start-Sleep -Seconds ([int]$arg) }
        default { throw "Unknown step '$name'" }
    }
}

# Compress saves settings, so every run restores them, whatever happens.
$config = Join-Path $env:LOCALAPPDATA 'miCompressor\user_config'
$backup = Join-Path $OutDir 'user_config.backup'
if (Test-Path $backup) { Remove-Item $backup -Recurse -Force }
$hadConfig = Test-Path $config
if ($hadConfig) { Copy-Item $config $backup -Recurse }

$app = $null
$exitCode = 0
try {
    if ($SavedFormat -ge 0) {
        # The value is JSON inside a JSON string, so its quotes are the literal text ": keep them exactly.
        $file = Join-Path $config 'SavedOutputSettings.json'
        $text = [IO.File]::ReadAllText($file)
        $changed = [regex]::Replace($text, '(\\u0022Format\\u0022:)\d+', "`${1}$SavedFormat")
        [IO.File]::WriteAllText($file, $changed, (New-Object Text.UTF8Encoding($false)))
    }

    $app = Start-Process $Exe -ArgumentList "`"$ImagePath`"" -WorkingDirectory (Split-Path $Exe) -PassThru
    $w = $null
    $deadline = (Get-Date).AddSeconds(30)
    while (-not $w -and (Get-Date) -lt $deadline) {
        if ($app.HasExited) { throw "App exited during startup with code $($app.ExitCode)" }
        $w = $UI::RootElement.FindFirst($TS::Children, (Cond $UI::ProcessIdProperty $app.Id))
        if (-not $w) { Start-Sleep -Milliseconds 500 }
    }
    if (-not $w) { throw 'No window within 30s' }
    Start-Sleep -Seconds 5 # the window appears before its content
    $hwnd = [IntPtr]$w.Current.NativeWindowHandle
    Write-Output "App running, pid $($app.Id). Input: $ImagePath"

    foreach ($step in $Steps) { Run-Step $step }
}
catch {
    Write-Output "STEP FAILED: $($_.Exception.Message)"
    $exitCode = 1
}
finally {
    if ($app -and -not $app.HasExited) { $app.Kill(); $app.WaitForExit(5000) | Out-Null }
    if ($hadConfig) {
        Remove-Item $config -Recurse -Force
        Copy-Item $backup $config -Recurse
    }
    elseif (Test-Path $config) { Remove-Item $config -Recurse -Force }
    Write-Output "App stopped, saved settings restored."
}
exit $exitCode
