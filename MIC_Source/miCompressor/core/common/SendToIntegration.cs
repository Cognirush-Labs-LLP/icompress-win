using System;
using System.IO;


namespace miCompressor.core;

public static class SendToIntegration
{

    /// <summary>
    /// Add Shortcut to "Send To" list (folder) for current user. If shortcut already exists with same name, we don't change anything. 
    /// </summary>
    public static void AddToSendTo()
    {
        try
        {
            string sendToPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                             "Microsoft\\Windows\\SendTo");
            string shortcutPath = Path.Combine(sendToPath, "Mass Image Compressor.lnk");

            if (File.Exists(shortcutPath)) return; // Prevent duplicate shortcut

            string exePath = Environment.ProcessPath;

            // Ensure MIC's executable exists before proceeding
            if (!File.Exists(exePath))
                return;

            CreateShortcut(shortcutPath, exePath, "Mass Image Compressor");
        }
        catch
        {
            //Ignore. Access right or some other issue. User can do this manually. 
        }
    }

    /// <summary>
    /// Remove entry from "Send To" menu. 
    /// </summary>
    /// <returns></returns>
    public static bool RemoveFromSendTo()
    {
        try
        {
            string shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                               "Microsoft\\Windows\\SendTo", "Mass Image Compressor.lnk");

            if (File.Exists(shortcutPath))
                File.Delete(shortcutPath);
        }
        catch
        {
            return false;
        }
        return true;
    }


    private static void CreateShortcut(string shortcutPath, string targetPath, string description)
    {
        // Obtain the WScript.Shell COM object.
        Type shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null)
            throw new InvalidOperationException("WScript.Shell is not available.");

        dynamic shell = Activator.CreateInstance(shellType);

        // Create the shortcut.
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.Description = description;
        shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(targetPath);
        shortcut.IconLocation = targetPath + ",0";

        shortcut.Save();
    }
}
