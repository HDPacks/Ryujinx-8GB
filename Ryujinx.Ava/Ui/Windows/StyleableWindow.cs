﻿using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.IO;
using System.Reflection;

namespace Ryujinx.Ava.UI.Windows
{
    public class StyleableWindow : Window
    {
        public IBitmap IconImage { get; set; }

        public StyleableWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            TransparencyLevelHint = WindowTransparencyLevel.None;

            using Stream stream = Assembly.GetAssembly(typeof(Ryujinx.Ui.Common.Configuration.ConfigurationState)).GetManifestResourceStream("Ryujinx.Ui.Common.Resources.Logo_Ryujinx.png");

            Icon = new WindowIcon(stream);
            stream.Position = 0;
            IconImage = new Bitmap(stream);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.SystemChrome | ExtendClientAreaChromeHints.OSXThickTitleBar;
        }
    }
}