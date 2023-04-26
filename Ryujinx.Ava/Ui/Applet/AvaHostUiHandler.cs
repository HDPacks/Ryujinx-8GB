using Avalonia.Controls;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.HLE;
using Ryujinx.HLE.HOS.Applets;
using Ryujinx.HLE.HOS.Services.Am.AppletOE.ApplicationProxyService.ApplicationProxy.Types;
using Ryujinx.HLE.Ui;
using System;
using System.Threading;

namespace Ryujinx.Ava.UI.Applet
{
    internal class AvaHostUiHandler : IHostUiHandler
    {
        private readonly MainWindow _parent;

        public IHostUiTheme HostUiTheme { get; }

        public AvaHostUiHandler(MainWindow parent)
        {
            _parent = parent;

            HostUiTheme = new AvaloniaHostUiTheme(parent);
        }

        public bool DisplayMessageDialog(ControllerAppletUiArgs args)
        {
            string message = LocaleManager.Instance.UpdateAndGetDynamicValue(
                args.PlayerCountMin == args.PlayerCountMax ? LocaleKeys.DialogControllerAppletMessage : LocaleKeys.DialogControllerAppletMessagePlayerRange,
                args.PlayerCountMin == args.PlayerCountMax ? args.PlayerCountMin.ToString() : $"{args.PlayerCountMin}-{args.PlayerCountMax}",
                args.SupportedStyles,
                string.Join(", ", args.SupportedPlayers),
                args.IsDocked ? LocaleManager.Instance[LocaleKeys.DialogControllerAppletDockModeSet] : "");

            return DisplayMessageDialog(LocaleManager.Instance[LocaleKeys.DialogControllerAppletTitle], message);
        }

        public bool DisplayMessageDialog(string title, string message)
        {
            ManualResetEvent dialogCloseEvent = new(false);

            bool okPressed = false;

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    ManualResetEvent deferEvent = new(false);

                    bool opened = false;

                    _parent.Activate();

                    UserResult response = await ContentDialogHelper.ShowDeferredContentDialog(_parent,
                       title,
                       message,
                       "",
                       LocaleManager.Instance[LocaleKeys.DialogOpenSettingsWindowLabel],
                       "",
                       LocaleManager.Instance[LocaleKeys.SettingsButtonClose],
                       (int)Symbol.Important,
                       deferEvent,
                       async (window) =>
                       {
                           if (opened)
                           {
                               return;
                           }

                           opened = true;

                           _parent.SettingsWindow = new SettingsWindow(_parent.VirtualFileSystem, _parent.ContentManager);

                           await _parent.SettingsWindow.ShowDialog(window);

                           opened = false;
                       });

                    if (response == UserResult.Ok)
                    {
                        okPressed = true;
                    }

                    dialogCloseEvent.Set();
                }
                catch (Exception ex)
                {
                    await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogMessageDialogErrorExceptionMessage, ex));

                    dialogCloseEvent.Set();
                }
            });

            dialogCloseEvent.WaitOne();

            return okPressed;
        }

        public bool DisplayInputDialog(SoftwareKeyboardUiArgs args, out string userText)
        {
            ManualResetEvent dialogCloseEvent = new(false);

            bool okPressed = false;
            bool error = false;
            string inputText = args.InitialText ?? "";

            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    var response = await SwkbdAppletDialog.ShowInputDialog(_parent, LocaleManager.Instance[LocaleKeys.SoftwareKeyboard], args);

                    if (response.Result == UserResult.Ok)
                    {
                        inputText = response.Input;
                        okPressed = true;
                    }
                }
                catch (Exception ex)
                {
                    error = true;

                    await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogSoftwareKeyboardErrorExceptionMessage, ex));
                }
                finally
                {
                    dialogCloseEvent.Set();
                }
            });

            dialogCloseEvent.WaitOne();

            userText = error ? null : inputText;

            return error || okPressed;
        }

        public void ExecuteProgram(Switch device, ProgramSpecifyKind kind, ulong value)
        {
            device.Configuration.UserChannelPersistence.ExecuteProgram(kind, value);
            if (_parent.ViewModel.AppHost != null)
            {
                _parent.ViewModel.AppHost.Stop();
            }
        }

        public bool DisplayErrorAppletDialog(string title, string message, string[] buttons)
        {
            ManualResetEvent dialogCloseEvent = new(false);

            bool showDetails = false;

            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    ErrorAppletWindow msgDialog = new(_parent, buttons, message)
                    {
                        Title = title,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        Width = 400
                    };

                    object response = await msgDialog.Run();

                    if (response != null && buttons != null && buttons.Length > 1 && (int)response != buttons.Length - 1)
                    {
                        showDetails = true;
                    }

                    dialogCloseEvent.Set();

                    msgDialog.Close();
                }
                catch (Exception ex)
                {
                    dialogCloseEvent.Set();

                    await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogErrorAppletErrorExceptionMessage, ex));
                }
            });

            dialogCloseEvent.WaitOne();

            return showDetails;
        }

        public IDynamicTextInputHandler CreateDynamicTextInputHandler()
        {
            return new AvaloniaDynamicTextInputHandler(_parent);
        }
    }
}