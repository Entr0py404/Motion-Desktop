Imports System.IO
Imports AxWMPLib
Imports WindowsDisplayAPI

Public Class Form1
    Public Const HWND_BOTTOM = 1
    Public Const HWND_NOTOPMOST = -2
    Public Const HWND_TOP = 0
    Public Const HWND_TOPMOST = -1
    Public Const SWP_NOMOVE = &H2
    Public Const SWP_NOSIZE = &H1
    Public Const SWP_NOACTIVATE = &H10
    Public Const SWP_SHOWWINDOW = &H40
    Public Const SWP_NOREDRAW = &H8
    Public Const SWP_NOZORDER = &H4
    Public Const SWP_NOREPOSITION = &H200
    Declare Auto Function SetWindowPos Lib "user32" (ByVal hWnd As IntPtr, ByVal hWndInsertAfter As IntPtr, ByVal X As Integer, ByVal Y As Integer, ByVal cx As Integer, ByVal cy As Integer, ByVal uFlags As UInteger) As Boolean

    Public MYDisplay As Display = Display.GetDisplays(0)
    Public MYScreen As Screen = MYDisplay.GetScreen()

    Dim FormLoadLock As Boolean = True

    ' Form1 - Load
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        ContextMenuStrip1.Renderer = New ToolStripProfessionalRenderer(New ColorTable())

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

        DisplayToolStripComboBox.BeginUpdate()
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

        ResizeAndRePositionWindowAndPlayer()

        FormLoadLock = False

        LoadVideoLocationFile()
    End Sub

    ' SetFormPosition
    Public Sub SetFormPosition(ByVal hWnd As IntPtr, ByVal Position As IntPtr)
        SetWindowPos(hWnd, Position, 0, 0, 0, 0, SWP_NOMOVE Or SWP_NOSIZE Or SWP_SHOWWINDOW Or SWP_NOACTIVATE Or SWP_NOREDRAW) ' Or SWP_NOREPOSITION SWP_NOMOVE Or SWP_NOSIZE Or SWP_SHOWWINDOW Or SWP_NOACTIVATE Or SWP_NOREDRAW Or SWP_NOREPOSITION Or SWP_NOZORDER)
    End Sub

    ' ResizeAndRePositionWindowAndPlayer
    Private Sub ResizeAndRePositionWindowAndPlayer()
        Me.Size = MYScreen.WorkingArea.Size
        Me.Location = New Point(MYScreen.WorkingArea.Left, MYScreen.WorkingArea.Top)
        SetFormPosition(Me.Handle, CType(HWND_BOTTOM, IntPtr))
        AxWindowsMediaPlayer1.Width = MYScreen.Bounds.Width
        AxWindowsMediaPlayer1.Height = MYScreen.Bounds.Height
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

    ' Form1 - LocationChanged
    Private Sub Form1_LocationChanged(sender As Object, e As EventArgs) Handles Me.LocationChanged
        ResizeAndRePositionWindowAndPlayer()
        'Console.WriteLine("Form1_LocationChanged")
    End Sub

    ' Form1 - Activated
    Private Sub Form1_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        SetFormPosition(Me.Handle, CType(HWND_BOTTOM, IntPtr))
        'Console.WriteLine("Me.Activated")
    End Sub

    ' Timer1 - Tick
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If AxWindowsMediaPlayer1.Ctlcontrols.currentPosition >= AxWindowsMediaPlayer1.Ctlcontrols.currentItem.duration - 0.1 Then
            AxWindowsMediaPlayer1.Ctlcontrols.currentPosition = 0
            'Console.WriteLine("Replay")
        End If
    End Sub

    '
    '
    '//ToolStripMenuItems
    '
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
        If Not strInput = "" Then
            AxWindowsMediaPlayer1.URL = strInput
            File.WriteAllText(Application.StartupPath & "\Video Location.cfg", strInput)
        End If
    End Sub

    ' OpenFileDialog1 - FileOk
    Private Sub OpenFileDialog1_FileOk(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles OpenFileDialog1.FileOk
        AxWindowsMediaPlayer1.URL = OpenFileDialog1.FileName
        File.WriteAllText(Application.StartupPath & "\Video Location.cfg", OpenFileDialog1.FileName)
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

    ' DisplayToolStripComboBox- SelectedIndexChanged
    Private Sub DisplayToolStripComboBox_SelectedIndexChanged(sender As Object, e As EventArgs) Handles DisplayToolStripComboBox.SelectedIndexChanged
        If Not DisplayToolStripComboBox.SelectedIndex = -1 And FormLoadLock = False Then
            MYDisplay = Display.GetDisplays(DisplayToolStripComboBox.SelectedIndex)
            MYScreen = MYDisplay.GetScreen()
            ResizeAndRePositionWindowAndPlayer()
            SetFormPosition(Me.Handle, CType(HWND_BOTTOM, IntPtr))
        End If
    End Sub

    ' ExitToolStripMenuItem - Click
    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        Me.Close()
    End Sub
End Class