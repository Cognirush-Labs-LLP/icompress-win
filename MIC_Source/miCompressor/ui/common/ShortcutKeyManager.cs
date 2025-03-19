using miCompressor.core;
using Microsoft.UI.Xaml.Input;
using System;

namespace miCompressor.ui;

/// <summary>
/// Manages global keyboard shortcut events.
/// Subscribers (e.g., UserControls) can add handlers to <see cref="KeyPressed"/>
/// and the page calls <see cref="Handle"/> when a key event occurs.
/// </summary>
public static class ShortcutKeyManager
{
    // A lock object to ensure thread-safe subscription and invocation.
    private static readonly object _lock = new object();

    // Private backing field for the event.
    private static EventHandler<ProcessKeyboardAcceleratorEventArgs>? _keyPressed;

    /// <summary>
    /// Occurs when a keyboard accelerator key is pressed.
    /// Make sure to unsubscribe from this event when the subscriber is disposed.
    /// </summary>
    public static event EventHandler<ProcessKeyboardAcceleratorEventArgs>? KeyPressed
    {
        add
        {
            lock (_lock)
            {
                _keyPressed += value;
            }
        }
        remove
        {
            lock (_lock)
            {
                _keyPressed -= value;
            }
        }
    }

    /// <summary>
    /// Invokes the KeyPressed event. Call this method from your Page when a keyboard event is received.
    /// </summary>
    /// <param name="e">The key event arguments.</param>
    public static void Handle(ProcessKeyboardAcceleratorEventArgs e)
    {
        EventHandler<ProcessKeyboardAcceleratorEventArgs>? handlers;
        lock (_lock)
        {
            handlers = _keyPressed;
        }

        if (handlers != null)
        {
            // Copy the invocation list to ensure thread safety during invocation.
            foreach (EventHandler<ProcessKeyboardAcceleratorEventArgs> handler in handlers.GetInvocationList())
            {
                try
                {
                    // Invoke the handler. Pass null as sender, or use a relevant context if needed.
                    handler?.Invoke(null, e);
                }
                catch (Exception ex)
                {
                    // Log the exception and continue with the next subscriber.
                    MicLog.Error($"Error in KeyPressed handler: {ex}");
                }
            }
        }
    }
}
