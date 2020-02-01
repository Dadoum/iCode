using System;
using System.Runtime.InteropServices;

namespace iCode.Native.Notify
{
    /// <summary>
    /// Class with native methods from libnotify
    /// </summary>
    public static class NativeMethods
    {
        /// <summary>
        /// The default expiration time on a notification.
        /// </summary>
        internal const int NOTIFY_EXPIRES_DEFAULT = -1;
        
        /// <summary>
        /// The notification never expires. It stays open until closed by the calling API or the user.
        /// </summary>
        internal const int NOTIFY_EXPIRES_NEVER = 0;
        
        /// <summary>
        /// The urgency level of the notification.
        /// </summary>
        internal enum NotifyUrgency
        {
            /// <summary>
            /// Low urgency. Used for unimportant notifications.
            /// </summary>
            NOTIFY_URGENCY_LOW,
            /// <summary>
            /// Normal urgency. Used for most standard notifications.
            /// </summary>
            NOTIFY_URGENCY_NORMAL,
            /// <summary>
            /// Critical urgency. Used for very important notifications.
            /// </summary>
            NOTIFY_URGENCY_CRITICAL
        }
        
        /// <summary>
        /// The real NotifyNotification struct
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct NotifyNotificationPrivate
        {
            /// <summary>
            /// The notification id
            /// </summary>
            public uint id;
            /// <summary>
            /// The AppName
            /// </summary>
            public IntPtr app_name;
            /// <summary>
            /// The summary
            /// </summary>
            public IntPtr summary;
            /// <summary>
            /// The body
            /// </summary>
            public IntPtr body;
            /// <summary>
            /// The icon name
            /// </summary>
            public IntPtr icon_name;
            /// <summary>
            /// The timeout
            /// </summary>
            public int timeout;
        }
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NotifyActionCallback(IntPtr notification, string action, IntPtr userData);
        
        #region C functions
        
        /// <summary>
        /// Gets the application name registered.
        /// </summary>
        /// <returns>The registered application name, passed to <see cref="notify_init"/>.</returns>
        [DllImport("libnotify.so.4", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        internal static extern string notify_get_app_name();
        
        /// <summary>
        /// Initialized libnotify. This must be called before any other functions.
        /// </summary>
        /// <param name="appName">The name of the application initializing libnotify.</param>
        /// <returns>True if successful or false on error</returns>
        [DllImport("libnotify.so.4", CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool notify_init([MarshalAs(UnmanagedType.LPStr)] string appName);
        
        /// <summary>
        /// Gets whether or not libnotify is initialized.
        /// </summary>
        /// <returns>True if libnotify is initialized, or false otherwise.</returns>
        [DllImport("libnotify.so.4", CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool notify_is_initted();
        
        [DllImport("libnotify.so.4", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void notify_notification_add_action(IntPtr notification,
                                                                   string action,
                                                                   string label, 
                                                                   NotifyActionCallback callback, 
                                                                   IntPtr userData, 
                                                                   Delegate freeFunc);
                                                                   
        /// <summary>
        /// Synchronously tells the notification server to hide the notification on the screen.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="error">The returned error information.</param>
        /// <returns>True on success, or false on error with error filled in</returns>
        [DllImport("libnotify.so.4", CharSet = CharSet.Ansi)]
        internal static extern bool notify_notification_close(IntPtr notification, out IntPtr error);
        
        /// <summary>
        /// Get a pointer to the NotifyNotificationPrivate-struct from the NotifyNotification pointer
        /// </summary>
        /// <param name="notification">A pointer to a NotifyNotification struct</param>
        /// <returns>A pointer to a NotifyNotificationPrivate struct</returns>
        [DllImport("libnotify_net_wrapper.so", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr notify_notification_get(IntPtr notification);
        
        /// <summary>
        /// Creates a new NotifyNotification. The summary text is required, but all other parameters are optional.
        /// </summary>
        /// <param name="summary">The required summary text.</param>
        /// <param name="body">The optional body text.</param>
        /// <param name="icon">The optional icon theme icon name or filename.</param>
        /// <returns>The new NotifyNotification.</returns>
        [DllImport("libnotify.so.4", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr notify_notification_new([MarshalAs(UnmanagedType.LPStr)] string summary,
                                                              [MarshalAs(UnmanagedType.LPStr)] string body,
                                                              [MarshalAs(UnmanagedType.LPStr)] string icon);

        /// <summary>
        /// Set Notification icon from Pixbuf
        /// </summary>
        [DllImport("libnotify.so.4", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void notify_notification_set_image_from_pixbuf(IntPtr notification,
                                                                            IntPtr pixbuf);

        /// <summary>
        /// Set Notification hint
        /// </summary>
        [DllImport("libnotify.so.4", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void notify_notification_set_hint(IntPtr notification,
                                                               [MarshalAs(UnmanagedType.LPStr)] string key,
                                                               IntPtr value);

        /// <summary>
        /// Set Notification icon from Pixbuf
        /// </summary>
        [DllImport("libnotify.so.4", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void notify_notification_add_action(IntPtr notification,
                                                                 [MarshalAs(UnmanagedType.LPStr)] string action,
                                                                 [MarshalAs(UnmanagedType.LPStr)] string label,
                                                                 NotifyActionCallback callback);
        
        /// <summary>
        /// Sets the application name for the notification.
        /// 
        /// If this function is not called or if app_name is NULL,
        /// the application name will be set from the value used in <see cref="notify_init()" /> or overridden with <see cref="notify_set_app_name()" />.
        /// </summary>
        /// <param name="notification">The notification</param>
        /// <param name="appName">The localised application name</param>
        [DllImport("libnotify.so.4", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void notify_notification_set_app_name(IntPtr notification, [MarshalAs(UnmanagedType.LPStr)] string appName);
                                                              
        /// <summary>
        /// Sets the urgency level of this notification.
        /// </summary>
        /// <param name="notification">The notification</param>
        /// <param name="urgency">The urgency level.</param>
        [DllImport("libnotify.so.4", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void notify_notification_set_urgency(IntPtr notification, NotifyUrgency urgency);
        
        /// <summary>
        /// Sets the timeout of the notification. 
        /// To set the default time, pass NOTIFY_EXPIRES_DEFAULT as timeout. To set the notification to never expire, pass NOTIFY_EXPIRES_NEVER.
        /// Note that the timeout may be ignored by the server.
        /// </summary>
        /// <param name="notification">The notification</param>
        /// <param name="timeout">The timeout in milliseconds.</param>
        [DllImport("libnotify.so.4", CharSet = CharSet.Ansi)]
        internal static extern void notify_notification_set_timeout(IntPtr notification, int timeout);
        
        /// <summary>
        /// Tells the notification server to display the notification on the screen.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="error">The returned error information.</param>
        /// <returns>True if successful. On error, this will return false and set error.</returns>
        [DllImport("libnotify.so.4", CharSet = CharSet.Ansi)]
        internal static extern bool notify_notification_show(IntPtr notification, out IntPtr error);
        
        /// <summary>
        /// Updates the notification text and icon. 
        /// This won't send the update out and display it on the screen. For that, you will need to call <see cref="notify_notification_show" />.
        /// </summary>
        /// <param name="notification">The notification to update.</param>
        /// <param name="summary">The new required summary text.</param>
        /// <param name="body">The optional body text.</param>
        /// <param name="icon">The optional icon theme, icon name or filename.</param>
        /// <returns></returns>
        [DllImport("libnotify.so.4", CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool notify_notification_update(IntPtr notification, 
                                                              [MarshalAs(UnmanagedType.LPStr)] string summary,
                                                              [MarshalAs(UnmanagedType.LPStr)] string body,
                                                              [MarshalAs(UnmanagedType.LPStr)] string icon);
        /// <summary>
        /// Uninitialized libnotify.
        /// This should be called when the program no longer needs libnotify for the rest of its lifecycle, typically just before exitting.
        /// </summary>
        [DllImport("libnotify.so.4")]
        internal static extern void notify_uninit();
        
        #endregion C functions
    }
}
