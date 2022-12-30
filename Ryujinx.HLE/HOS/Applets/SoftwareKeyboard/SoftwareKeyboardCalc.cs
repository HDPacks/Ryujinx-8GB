﻿using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Applets.SoftwareKeyboard
{
    /// <summary>
    /// A structure with configuration options of the software keyboard when starting a new input request in inline mode.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    struct SoftwareKeyboardCalc
    {
        public const int InputTextLength = SoftwareKeyboardCalcEx.InputTextLength;

        public uint Unknown;

        /// <summary>
        /// The size of the Calc struct, as reported by the process communicating with the applet.
        /// </summary>
        public ushort Size;

        public byte Unknown1;
        public byte Unknown2;

        /// <summary>
        /// Configuration flags. Each bit in the bitfield enabled a different operation of the keyboard
        /// using the data provided with the Calc structure.
        /// </summary>
        public KeyboardCalcFlags Flags;

        /// <summary>
        /// The original parameters used when initializing the keyboard applet.
        /// Flag: 0x1
        /// </summary>
        public SoftwareKeyboardInitialize Initialize;

        /// <summary>
        /// The audio volume used by the sound effects of the keyboard.
        /// Flag: 0x2
        /// </summary>
        public float Volume;

        /// <summary>
        /// The initial position of the text cursor (caret) in the provided input text.
        /// Flag: 0x10
        /// </summary>
        public int CursorPos;

        /// <summary>
        /// Appearance configurations for the on-screen keyboard.
        /// </summary>
        public SoftwareKeyboardAppear Appear;

        /// <summary>
        /// The initial input text to be used by the software keyboard.
        /// Flag: 0x8
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = InputTextLength + 1)]
        public string InputText;

        /// <summary>
        /// When set, the strings communicated by software keyboard will be encoded as UTF-8 instead of UTF-16.
        /// Flag: 0x20
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool UseUtf8;

        public byte Unknown3;

        /// <summary>
        /// [5.0.0+] Enable the backspace key in the software keyboard.
        /// Flag: 0x8000
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool BackspaceEnabled;

        public short Unknown4;
        public byte Unknown5;

        /// <summary>
        /// Flag: 0x200
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool KeytopAsFloating;

        /// <summary>
        /// Flag: 0x100
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool FooterScalable;

        /// <summary>
        /// Flag: 0x100
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool AlphaEnabledInInputMode;

        /// <summary>
        /// Flag: 0x100
        /// </summary>
        public byte InputModeFadeType;

        /// <summary>
        /// When set, the software keyboard ignores touch input.
        /// Flag: 0x200
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool TouchDisabled;

        /// <summary>
        /// When set, the software keyboard ignores hardware keyboard commands.
        /// Flag: 0x800
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool HardwareKeyboardDisabled;

        public uint Unknown6;
        public uint Unknown7;

        /// <summary>
        /// Default value is 1.0.
        /// Flag: 0x200
        /// </summary>
        public float KeytopScale0;

        /// <summary>
        /// Default value is 1.0.
        /// Flag: 0x200
        /// </summary>
        public float KeytopScale1;

        public float KeytopTranslate0;
        public float KeytopTranslate1;

        /// <summary>
        /// Default value is 1.0.
        /// Flag: 0x100
        /// </summary>
        public float KeytopBgAlpha;

        /// <summary>
        /// Default value is 1.0.
        /// Flag: 0x100
        /// </summary>
        public float FooterBgAlpha;

        /// <summary>
        /// Default value is 1.0.
        /// Flag: 0x200
        /// </summary>
        public float BalloonScale;

        public float Unknown8;
        public uint Unknown9;
        public uint Unknown10;
        public uint Unknown11;

        /// <summary>
        /// [5.0.0+] Enable sound effect.
        /// Flag: Enable:  0x2000
        ///       Disable: 0x4000
        /// </summary>
        public byte SeGroup;

        /// <summary>
        /// [6.0.0+] Enables the Trigger field when Trigger is non-zero.
        /// </summary>
        public byte TriggerFlag;

        /// <summary>
        /// [6.0.0+] Always set to zero.
        /// </summary>
        public byte Trigger;

        public byte Padding;

        public SoftwareKeyboardCalcEx ToExtended()
        {
            SoftwareKeyboardCalcEx calc = new SoftwareKeyboardCalcEx();

            calc.Unknown                 = Unknown;
            calc.Size                    = Size;
            calc.Unknown1                = Unknown1;
            calc.Unknown2                = Unknown2;
            calc.Flags                   = Flags;
            calc.Initialize              = Initialize;
            calc.Volume                  = Volume;
            calc.CursorPos               = CursorPos;
            calc.Appear                  = Appear.ToExtended();
            calc.InputText               = InputText;
            calc.UseUtf8                 = UseUtf8;
            calc.Unknown3                = Unknown3;
            calc.BackspaceEnabled        = BackspaceEnabled;
            calc.Unknown4                = Unknown4;
            calc.Unknown5                = Unknown5;
            calc.KeytopAsFloating        = KeytopAsFloating;
            calc.FooterScalable          = FooterScalable;
            calc.AlphaEnabledInInputMode = AlphaEnabledInInputMode;
            calc.InputModeFadeType        = InputModeFadeType;
            calc.TouchDisabled            = TouchDisabled;
            calc.HardwareKeyboardDisabled = HardwareKeyboardDisabled;
            calc.Unknown6                 = Unknown6;
            calc.Unknown7                 = Unknown7;
            calc.KeytopScale0             = KeytopScale0;
            calc.KeytopScale1             = KeytopScale1;
            calc.KeytopTranslate0         = KeytopTranslate0;
            calc.KeytopTranslate1         = KeytopTranslate1;
            calc.KeytopBgAlpha            = KeytopBgAlpha;
            calc.FooterBgAlpha            = FooterBgAlpha;
            calc.BalloonScale             = BalloonScale;
            calc.Unknown8                 = Unknown8;
            calc.Unknown9                 = Unknown9;
            calc.Unknown10                = Unknown10;
            calc.Unknown11                = Unknown11;
            calc.SeGroup                  = SeGroup;
            calc.TriggerFlag              = TriggerFlag;
            calc.Trigger                  = Trigger;

            return calc;
        }
    }
}
