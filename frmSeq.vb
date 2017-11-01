﻿
Public Class frmSeq

    Private ReadOnly Sequences As New List(Of Sequence)
    Private ActiveSequence As Sequence, SequencePosition As Decimal
    Private ReadOnly SeqControllers As New List(Of Integer)

    Public ReadOnly Property SeqController(cIdx As Integer) As Integer
        Get
            Return SeqControllers(IIf(rdFlood.Checked, 0, cIdx))
        End Get
    End Property

    Public Sub Init(pSequenceName As String, pBaseSpeed As Integer, pSoundSpeed As Integer, pMode As String)
        If SeqControllers.Count = 0 Then
            ' Inits 4 controller channels:
            For cIdx As Integer = 0 To 3 : SeqControllers.Add(0) : Next

            ' Create the available Sequences:
            Dim tmpS As Sequence

            tmpS = New Sequence With {.Name = "Rainbow", .Distance = 256 / 4, .SpeedFactor = 1, .PreviewMode = Sequence.PreviewModes.Color}
            For i As Integer = 0 To 255 : tmpS.Values.Add(i) : Next i
            Sequences.Add(tmpS)

            tmpS = New Sequence With {.Name = "Wave", .Distance = 256 / 4, .SpeedFactor = 1, .PreviewMode = Sequence.PreviewModes.PowerColor}
            For i As Integer = 0 To 255 : tmpS.Values.Add(127 + Math.Sin(i / 255 * Math.PI * 2) * 127) : Next i
            Sequences.Add(tmpS)

            tmpS = New Sequence With {.Name = "Chase soft", .Distance = 256 / 4, .SpeedFactor = 1, .PreviewMode = Sequence.PreviewModes.PowerColor}
            For i As Integer = 0 To 64 : tmpS.Values.Add(Math.Sin(i / 64 * Math.PI) * 255) : Next i : For i As Integer = 65 To 255 : tmpS.Values.Add(0) : Next i
            Sequences.Add(tmpS)

            tmpS = New Sequence With {.Name = "Chase", .Distance = 1, .SpeedFactor = 0.02, .PreviewMode = Sequence.PreviewModes.Power}
            tmpS.Values.Add(255) : tmpS.Values.Add(0) : tmpS.Values.Add(0) : tmpS.Values.Add(0)
            Sequences.Add(tmpS)

            ' Init the Form:
            lstSeq.Items.AddRange(Sequences.ToArray)
            If lstSeq.Items.Count > 0 Then lstSeq.SelectedIndex = 0
            Me.TopMost = True
        End If

        ' LoadConfiguration:
        For Each s In lstSeq.Items
            If DirectCast(s, Sequence).Name = pSequenceName Then
                lstSeq.SelectedItem = s
                Exit For
            End If
        Next
        trSpeed.Value = pBaseSpeed
        txtSound.Text = pSoundSpeed
        If Not String.IsNullOrEmpty(pMode) Then
            If pMode.ToUpper = "FLOOD" Then rdFlood.Checked = True
            If pMode.ToUpper = "CHASE" Then rdChase.Checked = True
        End If
    End Sub


    Friend Function Serialize() As XElement
        Dim res As New XElement("Sequencer",
            New XElement("ActiveSequence", ActiveSequence.Name),
            New XElement("BaseSpeed", trSpeed.Value),
            New XElement("SoundSpeed", TextBoxValue(txtSound)),
            New XElement("Mode", IIf(rdFlood.Checked, "Flood", "Chase"))
        )
        Return res
    End Function


    Public Sub Advance()
        If ActiveSequence Is Nothing Then Return
        Dim tmpSpeed As Integer = Math.Min(450, trSpeed.Value + TextBoxValue(txtSound) / 60 * _MainForm.FrmSound.SoundController)
        Using g As Graphics = trSpeed.CreateGraphics
            Dim l As Integer = Math.Min(255, tmpSpeed) / 255 * (trSpeed.Height - 12 - 12)
            g.DrawLine(Pens.Orange, trSpeed.Width - 15, trSpeed.Height - 12, trSpeed.Width - 15, trSpeed.Height - 12 - l)
            g.DrawLine(Pens.DarkRed, trSpeed.Width - 15, trSpeed.Height - 12 - l, trSpeed.Width - 15, 12)
        End Using

        SequencePosition += tmpSpeed ^ 1.5 / 80 * ActiveSequence.SpeedFactor ' speed=255 <=> 4 full cycles per second @ timer.interval=50ms
        SequencePosition = TurnAround(SequencePosition)
        Dim i As Integer = Math.Truncate(SequencePosition)
        Dim sweepAngle As Single = IIf(rdFlood.Checked, 360, 360 / SeqControllers.Count)
        With Panel1.CreateGraphics
            For cIdx As Integer = 0 To IIf(rdFlood.Checked, 0, SeqControllers.Count - 1)
                SeqControllers(cIdx) = ActiveSequence.Values(i)
                Dim rgb As RGB
                Select Case ActiveSequence.PreviewMode
                    Case Sequence.PreviewModes.Color
                        rgb = HSVtoRGB(SeqController(cIdx) / 255 * 360, 100, 100)
                    Case Sequence.PreviewModes.PowerColor
                        rgb = HSVtoRGB(SeqController(cIdx) / 255 * 360, 100, 10 + SeqController(cIdx) / 255 * 90)
                    Case Else
                        rgb = New RGB With {.r = SeqController(cIdx), .g = SeqController(cIdx), .b = SeqController(cIdx)}
                End Select
                .FillPie(New SolidBrush(Color.FromArgb(rgb.r, rgb.g, rgb.b)), Panel1.ClientRectangle, sweepAngle * (cIdx - 2), sweepAngle)
                i = TurnAround(i + ActiveSequence.Distance)
            Next
        End With
    End Sub

    Private Function TurnAround(pInput As Decimal) As Decimal
        Do While Math.Truncate(pInput) < 0
            pInput += ActiveSequence.Values.Count
        Loop
        Do While Math.Truncate(pInput) > ActiveSequence.Values.Count - 1
            pInput -= ActiveSequence.Values.Count
        Loop
        Return pInput
    End Function

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        Me.Visible = False
        e.Cancel = True
    End Sub


    Private Sub frmSeq_Paint(sender As Object, e As PaintEventArgs) Handles Me.Paint
        If ActiveSequence Is Nothing Then Return
        With e.Graphics
            Dim xcenter As Decimal = Me.ClientRectangle.Width / 2, ycenter As Decimal = Me.ClientRectangle.Height / 2, angle As Decimal
            .Clear(Me.BackColor)
            For i As Decimal = 0 To ActiveSequence.Values.Count - 1
                angle = i / ActiveSequence.Values.Count * Math.PI * 2
                Dim rgb As RGB = HSVtoRGB(ActiveSequence.Values(i) / 255 * 360, 100, 100)
                .DrawLine(New Pen(Color.FromArgb(rgb.r, rgb.g, rgb.b), 7), xcenter + CInt(xcenter * 0.9 * Math.Cos(angle)), ycenter + CInt(ycenter * 0.9 * Math.Sin(angle)), xcenter + CInt(xcenter * Math.Cos(angle)), ycenter + CInt(ycenter * Math.Sin(angle)))
                .DrawLine(New Pen(Color.FromArgb(ActiveSequence.Values(i), ActiveSequence.Values(i), ActiveSequence.Values(i)), 6), xcenter + CInt(xcenter * 0.8 * Math.Cos(angle)), ycenter + CInt(ycenter * 0.8 * Math.Sin(angle)), xcenter + CInt(xcenter * 0.9 * Math.Cos(angle)), ycenter + CInt(ycenter * 0.9 * Math.Sin(angle)))
            Next
        End With
    End Sub

    Private Sub lbSound_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles lbSound.LinkClicked
        _MainForm.FrmSound.Visible = True
    End Sub

    Private Sub lstSeq_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lstSeq.SelectedIndexChanged
        Dim s = lstSeq.SelectedItem
        If s Is Nothing Then Return
        SequencePosition = 0
        ActiveSequence = s
        Me.Refresh()
    End Sub


    Private Class Sequence
        Public Name As String
        Public ReadOnly Values As New List(Of Integer)
        Public Distance As Integer
        Public SpeedFactor As Decimal
        Public PreviewMode As PreviewModes
        Public Enum PreviewModes
            Power
            PowerColor
            Color
        End Enum
        Public Overrides Function ToString() As String
            Return Name
        End Function
    End Class


End Class