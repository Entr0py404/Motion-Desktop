
Public Class Form1
    <DllImport("user32.dll", SetLastError:=True, CharSet:=CharSet.Auto)>
    Private Shared Function FindWindow(lpClassName As String, lpWindowName As String) As IntPtr
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function FindWindowEx(hWndParent As IntPtr, hWndChildAfter As IntPtr, lpszClass As String, lpszWindow As String) As IntPtr
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function SetParent(hWndChild As IntPtr, hWndNewParent As IntPtr) As IntPtr
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function SetWindowPos(hWnd As IntPtr, hWndInsertAfter As IntPtr, X As Integer, Y As Integer, cx As Integer, cy As Integer, uFlags As UInteger) As Boolean
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function GetShellWindow() As IntPtr
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function ShowWindow(hWnd As IntPtr, nCmdShow As Integer) As Boolean
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function SendMessageTimeout(hWnd As IntPtr, Msg As UInteger, wParam As IntPtr, lParam As IntPtr, flags As SendMessageTimeoutFlags, timeout As UInteger, ByRef pdwResult As IntPtr) As IntPtr
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Private Shared Function SystemParametersInfo(uAction As UInteger, uParam As UInteger, lpvParam As String, fuWinIni As UInteger) As Boolean
    End Function

    Private Const SPI_GETDESKWALLPAPER As UInteger = &H73
    Private Const SPI_SETDESKWALLPAPER As UInteger = &H14
    Private Const SPIF_UPDATEINIFILE As UInteger = &H1
    Private Const SPIF_SENDWININICHANGE As UInteger = &H2
    Private originalWallpaper As String = New String(" "c, 260)

    <Flags>
    Private Enum SendMessageTimeoutFlags As UInteger
        SMTO_NORMAL = &H0
        SMTO_BLOCK = &H1
        SMTO_ABORTIFHUNG = &H2
        SMTO_NOTIMEOUTIFNOTHUNG = &H8
        SMTO_ERRORONEXIT = &H20
    End Enum

    Private Const SWP_NOZORDER As UInteger = &H4
    Private Const SWP_SHOWWINDOW As UInteger = &H40
    Private Const SW_HIDE As Integer = 0
    Public Const SWP_NOACTIVATE As UInteger = &H10
    Public MYDisplay As Display = Display.GetDisplays(0)
    Public MYScreen As Screen = MYDisplay.GetScreen()
    Dim FormLoadLock As Boolean = True

    ' Form1 - Load
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Get the current wallpaper
        SystemParametersInfo(SPI_GETDESKWALLPAPER, 260, originalWallpaper, 0)

        ContextMenuStrip1.Renderer = New ToolStripProfessionalRenderer(New ColorTable())

        ' Initialize media player settings
        AxWindowsMediaPlayer1.settings.volume = 100
        AxWindowsMediaPlayer1.settings.mute = True
        AxWindowsMediaPlayer1.uiMode = "none"
        AxWindowsMediaPlayer1.Ctlenabled = False
        AxWindowsMediaPlayer1.enableContextMenu = False
        AxWindowsMediaPlayer1.stretchToFit = True
        AxWindowsMediaPlayer1.windowlessVideo = True
        AxWindowsMediaPlayer1.settings.autoStart = True
        AxWindowsMediaPlayer1.UseWaitCursor = False
        AxWindowsMediaPlayer1.settings.setMode("loop", True)

        OpenFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)

        ' Get the Progman window handle
        Dim progman As IntPtr = FindWindow("Progman", Nothing)

        ' Send a message to Progman to spawn a WorkerW
        Dim result As IntPtr = IntPtr.Zero
        Dim msg As UInteger = &H52C  ' The message to spawn a WorkerW
        SendMessageTimeout(progman, msg, IntPtr.Zero, IntPtr.Zero, SendMessageTimeoutFlags.SMTO_NORMAL, 1000, result)

        ' Find the WorkerW window handle
        Dim workerw As IntPtr = IntPtr.Zero
        Dim desktopHandle As IntPtr = FindWindow("Progman", Nothing)
        workerw = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "WorkerW", Nothing)
        While (workerw <> IntPtr.Zero)
            Dim shellviewWin As IntPtr = FindWindowEx(workerw, IntPtr.Zero, "SHELLDLL_DefView", Nothing)
            If (shellviewWin <> IntPtr.Zero) Then
                desktopHandle = FindWindowEx(IntPtr.Zero, workerw, "WorkerW", Nothing)
            End If
            workerw = FindWindowEx(IntPtr.Zero, workerw, "WorkerW", Nothing)
        End While

        'UpdateDisplayList()

        ' Set the form as a child of the WorkerW window
        SetParent(Me.Handle, desktopHandle)

        ' Start the video
        LoadVideoLocationFile()

        ' Attach DisplaySettingsChanged event handler to handle changes in display settings
        AddHandler SystemEvents.DisplaySettingsChanged, AddressOf DisplaySettingsChanged

        FormLoadLock = False
    End Sub

    ' UpdateDisplayList
    Private Sub UpdateDisplayList()
        Console.WriteLine("UpdateDisplayList()")
        ' Unsubscribe from SelectedIndexChanged temporarily to prevent it from triggering
        RemoveHandler DisplayToolStripComboBox.SelectedIndexChanged, AddressOf DisplayToolStripComboBox_SelectedIndexChanged

        DisplayToolStripComboBox.BeginUpdate()
        DisplayToolStripComboBox.Items.Clear()
        For Each Display As Display In Display.GetDisplays()
            If Display.IsGDIPrimary Then
                DisplayToolStripComboBox.Items.Add(Display.ToPathDisplayTarget.FriendlyName & " (Primary)")
                DisplayToolStripComboBox.SelectedIndex = DisplayToolStripComboBox.Items.Count - 1
            Else
                DisplayToolStripComboBox.Items.Add(Display.ToPathDisplayTarget.FriendlyName)
            End If
        Next
        DisplayToolStripComboBox.EndUpdate()

        MYDisplay = Display.GetDisplays(DisplayToolStripComboBox.SelectedIndex)
        MYScreen = MYDisplay.GetScreen()

        NotifyIcon1.Text = "Motion Desktop - " & MYDisplay.ToPathDisplayTarget.FriendlyName

        ' Re-subscribe to SelectedIndexChanged after updating
        AddHandler DisplayToolStripComboBox.SelectedIndexChanged, AddressOf DisplayToolStripComboBox_SelectedIndexChanged
    End Sub

    ' AxWindowsMediaPlayer1 - PlayStateChange
    Private Sub AxWindowsMediaPlayer1_PlayStateChange(sender As Object, e As _WMPOCXEvents_PlayStateChangeEvent) Handles AxWindowsMediaPlayer1.PlayStateChange
        If e.newState = 3 Then
            Timer1.Enabled = True
            If MuteToolStripMenuItem.Checked Then
                AxWindowsMediaPlayer1.settings.mute = True
            End If
        Else
            Timer1.Enabled = False
        End If
    End Sub

    ' LoadVideoLocationFile()
    Public Sub LoadVideoLocationFile()
        If File.Exists(Application.StartupPath & "\Video Location.cfg") Then
            Dim VideoLocationFile() As String = File.ReadAllLines(Application.StartupPath & "\Video Location.cfg")
            If File.Exists(VideoLocationFile(0)) Then
                AxWindowsMediaPlayer1.URL = VideoLocationFile(0)
                Cursor.Current = Cursors.Default
            Else
                If File.Exists(Application.StartupPath & "\Default.mp4") Then
                    AxWindowsMediaPlayer1.URL = Application.StartupPath & "\Default.mp4"
                    Cursor.Current = Cursors.Default
                End If
            End If
        Else
            If File.Exists(Application.StartupPath & "\Default.mp4") Then
                AxWindowsMediaPlayer1.URL = Application.StartupPath & "\Default.mp4"
                Cursor.Current = Cursors.Default
            End If
        End If
    End Sub

    ' Timer1 - Tick
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If AxWindowsMediaPlayer1.playState = WMPLib.WMPPlayState.wmppsPlaying Then
            Dim currentPosition As Double = AxWindowsMediaPlayer1.Ctlcontrols.currentPosition
            Dim duration As Double = AxWindowsMediaPlayer1.currentMedia.duration
            If duration > 0 AndAlso currentPosition >= duration - 0.05 Then
                AxWindowsMediaPlayer1.Ctlcontrols.currentPosition = 0
            End If
        End If
    End Sub

    ' ResizeAndRePositionWindowAndPlayer
    Private Sub ResizeAndRePositionWindowAndPlayer()
        Console.WriteLine("ResizeAndRePositionWindowAndPlayer()")
        Me.Size = MYScreen.Bounds.Size
        Me.Location = New Point(MYScreen.Bounds.Left, MYScreen.Bounds.Top)
        SetWindowPos(Me.Handle, CType(1, IntPtr), MYScreen.Bounds.Left, MYScreen.Bounds.Top, MYScreen.Bounds.Width, MYScreen.Bounds.Height, SWP_NOZORDER Or SWP_SHOWWINDOW Or SWP_NOACTIVATE)
    End Sub

    ' Restore the original wallpaper
    Private Sub RestoreWallpaper()
        SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, originalWallpaper, SPIF_UPDATEINIFILE Or SPIF_SENDWININICHANGE)
        Console.WriteLine("RestoreWallpaper()")
    End Sub

    ' DisplaySettingsChanged()
    Public Sub DisplaySettingsChanged(ByVal sender As Object, ByVal e As EventArgs)
        If Not FormLoadLock Then
            Console.WriteLine("DisplaySettingsChanged")
            UpdateDisplayList()
            ResizeAndRePositionWindowAndPlayer()
        End If
    End Sub

    ' Detach from WorkerW
    Private Sub DetachFromWorkerW()
        ' Get the original desktop handle
        Dim desktopHandle As IntPtr = GetShellWindow()
        ' Set the form's parent back to the original desktop
        SetParent(Me.Handle, desktopHandle)
        ' Ensure the form is hidden before closing
        ShowWindow(Me.Handle, SW_HIDE)
    End Sub

    ' Override OnFormClosing
    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        ' Detach the form from WorkerW
        DetachFromWorkerW()
        ' Restore the original wallpaper
        RestoreWallpaper()
        ' Call the base method
        MyBase.OnFormClosing(e)
    End Sub

    '
    ' ToolStripMenuItems
    '
    ' PausePlayToolStripMenuItem - Click
    Private Sub PausePlayToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles PausePlayToolStripMenuItem.Click
        If Not AxWindowsMediaPlayer1.playState = WMPLib.WMPPlayState.wmppsPaused Then
            AxWindowsMediaPlayer1.Ctlcontrols.pause()
            Timer1.Enabled = False
            PausePlayToolStripMenuItem.Checked = True
        Else
            AxWindowsMediaPlayer1.Ctlcontrols.play()
            Timer1.Enabled = True
            PausePlayToolStripMenuItem.Checked = False
        End If
    End Sub

    ' FromFileToolStripMenuItem - Click
    Private Sub FromFileToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FromFileToolStripMenuItem.Click
        OpenFileDialog1.ShowDialog()
    End Sub

    ' FromURLToolStripMenuItem - Click
    Private Sub FromURLToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FromURLToolStripMenuItem.Click
        Dim strInput As String
        strInput = InputBox("Set Video URL", "Video URL")
        If strInput.StartsWith("https://") Or strInput.StartsWith("http://") Then
            AxWindowsMediaPlayer1.URL = strInput
            File.WriteAllText(Application.StartupPath & "\Video Location.cfg", strInput)
        End If
    End Sub

    ' OpenFileDialog1 - FileOk
    Private Sub OpenFileDialog1_FileOk(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles OpenFileDialog1.FileOk
        AxWindowsMediaPlayer1.URL = OpenFileDialog1.FileName
        File.WriteAllText(Application.StartupPath & "\Video Location.cfg", OpenFileDialog1.FileName)
    End Sub

    ' DefaultToolStripMenuItem - Click
    Private Sub DefaultToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DefaultToolStripMenuItem.Click
        Dim DefaultVideo As String = Application.StartupPath & "\Default.mp4"
        If File.Exists(DefaultVideo) Then
            AxWindowsMediaPlayer1.URL = DefaultVideo
            File.WriteAllText(Application.StartupPath & "\Video Location.cfg", DefaultVideo)
        End If
    End Sub

    ' MuteToolStripMenuItem - Click
    Private Sub MuteToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MuteToolStripMenuItem.Click
        If AxWindowsMediaPlayer1.settings.mute = True Then
            AxWindowsMediaPlayer1.settings.mute = False
            MuteToolStripMenuItem.Checked = False
        Else
            AxWindowsMediaPlayer1.settings.mute = True
            MuteToolStripMenuItem.Checked = True
        End If
    End Sub

    ' DisplayToolStripComboBox - SelectedIndexChanged
    Private Sub DisplayToolStripComboBox_SelectedIndexChanged(sender As Object, e As EventArgs) Handles DisplayToolStripComboBox.SelectedIndexChanged
        If DisplayToolStripComboBox.SelectedIndex <> -1 AndAlso FormLoadLock = False Then
            MYDisplay = Display.GetDisplays(DisplayToolStripComboBox.SelectedIndex)
            MYScreen = MYDisplay.GetScreen()
            ResizeAndRePositionWindowAndPlayer()
            RestoreWallpaper()
            Console.WriteLine("DisplayToolStripComboBox.SelectedIndexChanged")
        End If
    End Sub

    ' ExitToolStripMenuItem - Click
    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        Me.Close()
    End Sub
End Class