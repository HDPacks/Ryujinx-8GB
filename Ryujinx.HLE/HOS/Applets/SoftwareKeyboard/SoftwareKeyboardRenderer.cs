﻿using Ryujinx.HLE.Ui;
using Ryujinx.Memory;
using System;
using System.Threading;

namespace Ryujinx.HLE.HOS.Applets.SoftwareKeyboard
{
    /// <summary>
    /// Class that manages the renderer base class and its state in a multithreaded context.
    /// </summary>
    internal class SoftwareKeyboardRenderer : IDisposable
    {
        private const int TextBoxBlinkSleepMilliseconds   = 100;
        private const int RendererWaitTimeoutMilliseconds = 100;

        private readonly object _stateLock  = new object();

        private SoftwareKeyboardUiState      _state = new SoftwareKeyboardUiState();
        private SoftwareKeyboardRendererBase _renderer;

        private TimedAction _textBoxBlinkTimedAction = new TimedAction();
        private TimedAction _renderAction            = new TimedAction();

        public SoftwareKeyboardRenderer(IHostUiTheme uiTheme)
        {
            _renderer = new SoftwareKeyboardRendererBase(uiTheme);

            StartTextBoxBlinker(_textBoxBlinkTimedAction, _state, _stateLock);
            StartRenderer(_renderAction, _renderer, _state, _stateLock);
        }

        private static void StartTextBoxBlinker(TimedAction timedAction, SoftwareKeyboardUiState state, object stateLock)
        {
            timedAction.Reset(() =>
            {
                lock (stateLock)
                {
                    // The blinker is on half of the time and events such as input
                    // changes can reset the blinker.
                    state.TextBoxBlinkCounter = (state.TextBoxBlinkCounter + 1) % (2 * SoftwareKeyboardRendererBase.TextBoxBlinkThreshold);

                    // Tell the render thread there is something new to render.
                    Monitor.PulseAll(stateLock);
                }
            }, TextBoxBlinkSleepMilliseconds);
        }

        private static void StartRenderer(TimedAction timedAction, SoftwareKeyboardRendererBase renderer, SoftwareKeyboardUiState state, object stateLock)
        {
            SoftwareKeyboardUiState internalState = new SoftwareKeyboardUiState();

            bool canCreateSurface = false;
            bool needsUpdate      = true;

            timedAction.Reset(() =>
            {
                lock (stateLock)
                {
                    if (!Monitor.Wait(stateLock, RendererWaitTimeoutMilliseconds))
                    {
                        return;
                    }

                    needsUpdate  = UpdateStateField(ref state.InputText,           ref internalState.InputText);
                    needsUpdate |= UpdateStateField(ref state.CursorBegin,         ref internalState.CursorBegin);
                    needsUpdate |= UpdateStateField(ref state.CursorEnd,           ref internalState.CursorEnd);
                    needsUpdate |= UpdateStateField(ref state.AcceptPressed,       ref internalState.AcceptPressed);
                    needsUpdate |= UpdateStateField(ref state.CancelPressed,       ref internalState.CancelPressed);
                    needsUpdate |= UpdateStateField(ref state.OverwriteMode,       ref internalState.OverwriteMode);
                    needsUpdate |= UpdateStateField(ref state.TypingEnabled,       ref internalState.TypingEnabled);
                    needsUpdate |= UpdateStateField(ref state.ControllerEnabled,   ref internalState.ControllerEnabled);
                    needsUpdate |= UpdateStateField(ref state.TextBoxBlinkCounter, ref internalState.TextBoxBlinkCounter);

                    canCreateSurface = state.SurfaceInfo != null && internalState.SurfaceInfo == null;

                    if (canCreateSurface)
                    {
                        internalState.SurfaceInfo = state.SurfaceInfo;
                    }
                }

                if (canCreateSurface)
                {
                    renderer.CreateSurface(internalState.SurfaceInfo);
                }

                if (needsUpdate)
                {
                    renderer.DrawMutableElements(internalState);
                    renderer.CopyImageToBuffer();
                    needsUpdate = false;
                }
            });
        }

        private static bool UpdateStateField<T>(ref T source, ref T destination) where T : IEquatable<T>
        {
            if (!source.Equals(destination))
            {
                destination = source;
                return true;
            }

            return false;
        }

#pragma warning disable CS8632
        public void UpdateTextState(string? inputText, int? cursorBegin, int? cursorEnd, bool? overwriteMode, bool? typingEnabled)
#pragma warning restore CS8632
        {
            lock (_stateLock)
            {
                // Update the parameters that were provided.
                _state.InputText     = inputText != null ? inputText : _state.InputText;
                _state.CursorBegin   = cursorBegin.GetValueOrDefault(_state.CursorBegin);
                _state.CursorEnd     = cursorEnd.GetValueOrDefault(_state.CursorEnd);
                _state.OverwriteMode = overwriteMode.GetValueOrDefault(_state.OverwriteMode);
                _state.TypingEnabled = typingEnabled.GetValueOrDefault(_state.TypingEnabled);

                // Reset the cursor blink.
                _state.TextBoxBlinkCounter = 0;

                // Tell the render thread there is something new to render.
                Monitor.PulseAll(_stateLock);
            }
        }

        public void UpdateCommandState(bool? acceptPressed, bool? cancelPressed, bool? controllerEnabled)
        {
            lock (_stateLock)
            {
                // Update the parameters that were provided.
                _state.AcceptPressed     = acceptPressed.GetValueOrDefault(_state.AcceptPressed);
                _state.CancelPressed     = cancelPressed.GetValueOrDefault(_state.CancelPressed);
                _state.ControllerEnabled = controllerEnabled.GetValueOrDefault(_state.ControllerEnabled);

                // Tell the render thread there is something new to render.
                Monitor.PulseAll(_stateLock);
            }
        }

        public void SetSurfaceInfo(RenderingSurfaceInfo surfaceInfo)
        {
            lock (_stateLock)
            {
                _state.SurfaceInfo = surfaceInfo;

                // Tell the render thread there is something new to render.
                Monitor.PulseAll(_stateLock);
            }
        }

        internal bool DrawTo(IVirtualMemoryManager destination, ulong position)
        {
            return _renderer.WriteBufferToMemory(destination, position);
        }

        public void Dispose()
        {
            _textBoxBlinkTimedAction.RequestCancel();
            _renderAction.RequestCancel();
        }
    }
}
