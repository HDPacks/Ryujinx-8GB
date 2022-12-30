namespace Ryujinx.HLE.HOS.Services.Am.AppletAE.AllSystemAppletProxiesService.SystemAppletProxy
{
    enum AppletMessage
    {
        None                             = 0,
        ChangeIntoForeground             = 1,
        ChangeIntoBackground             = 2,
        Exit                             = 4,
        ApplicationExited                = 6,
        FocusStateChanged                = 15,
        Resume                           = 16,
        DetectShortPressingHomeButton    = 20,
        DetectLongPressingHomeButton     = 21,
        DetectShortPressingPowerButton   = 22,
        DetectMiddlePressingPowerButton  = 23,
        DetectLongPressingPowerButton    = 24,
        RequestToPrepareSleep            = 25,
        FinishedSleepSequence            = 26,
        SleepRequiredByHighTemperature   = 27,
        SleepRequiredByLowBattery        = 28,
        AutoPowerDown                    = 29,
        OperationModeChanged             = 30,
        PerformanceModeChanged           = 31,
        DetectReceivingCecSystemStandby  = 32,
        SdCardRemoved                    = 33,
        LaunchApplicationRequested       = 50,
        RequestToDisplay                 = 51,
        ShowApplicationLogo              = 55,
        HideApplicationLogo              = 56,
        ForceHideApplicationLogo         = 57,
        FloatingApplicationDetected      = 60,
        DetectShortPressingCaptureButton = 90,
        AlbumScreenShotTaken             = 92,
        AlbumRecordingSaved              = 93
    }
}