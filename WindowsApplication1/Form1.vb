Imports ZXing
Imports ZXing.BarcodeReader
Imports Emgu
Imports Emgu.CV.Capture
Imports System.Net
Imports System.Web.Script.Serialization
Imports Newtonsoft.Json
Imports System.IO

Public Class MainApplication

    Dim thread As Threading.Thread
    Dim camcapture As Emgu.CV.Capture
    Dim decoder As ZXing.BarcodeReader
    Dim image, copy As Bitmap
    Dim result As Object

    Dim resultString As String

    Dim apifindclient As New WebClient
    Dim apisetclient As New WebClient
    Dim apifindresponse, apisetresponse As String

    Dim isBusy As Boolean = False

    Dim detailsurl As String = "http://localhost/feutechcs/public/getdetails/"
    Dim setarrivalurl As String = "http://localhost/feutechcs/public/setarrival/"

    Dim jss As New Newtonsoft.Json.JsonSerializer

    'participant details
    Dim fullname As String = ""
    Dim timearrived As String = ""


    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.CheckForIllegalCrossThreadCalls = False
        Me.StartPosition = FormStartPosition.WindowsDefaultLocation
        Me.PictureBox1.SizeMode = PictureBoxSizeMode.StretchImage
        Me.DataGridView1.Columns(0).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        Me.DataGridView1.Columns(1).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill

        thread = New Threading.Thread(AddressOf startCapture)
        thread.Start()
        Timer1.Start()
    End Sub

    Private Sub startCapture()
        camcapture = New CV.Capture(1)
        While True
            image = New Bitmap(camcapture.QueryFrame().Bitmap)
            If image IsNot Nothing Then
                PictureBox1.Image = image
            End If
        End While
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If thread.IsAlive Then
            Capture = Nothing
            thread.Abort()
        End If
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If isBusy = False Then
            If image IsNot Nothing Then
                resultString = decodeImage(image)
                If resultString IsNot Nothing Then
                    resultString = resultString.Trim(" ")
                    If resultString IsNot String.Empty Then
                        findAttendee(resultString)
                    End If
                End If
            End If
        End If
    End Sub

    Private Sub Button1_Click_1(sender As Object, e As EventArgs) Handles Button1.Click
        If fullname IsNot String.Empty AndAlso timearrived IsNot String.Empty Then
            TextBox1.Text = ""
            DataGridView1.Rows.Add(fullname, timearrived)
        End If
    End Sub

    Private Function decodeImage(b As Bitmap)
        Dim o As Object
        decoder = New ZXing.BarcodeReader
        o = decoder.Decode(b)
        If o IsNot Nothing Then
            Return o.ToString
        End If
        Return Nothing
    End Function

    Private Sub findAttendee(decoded As String)
        isBusy = True
        Dim qr As String = decoded
        apifindresponse = apifindclient.DownloadString(detailsurl + decoded)

        Dim searchDictionary As Dictionary(Of String, String) = jss.Deserialize(
            Of Dictionary(Of String, String))(New JsonTextReader(New StringReader(apifindresponse)))

        If searchDictionary.ContainsKey("error") Then
            MessageBox.Show(searchDictionary("error"), "", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Else
            apisetresponse = apifindclient.DownloadString(setarrivalurl + searchDictionary("id"))
            Dim findDictionary As Dictionary(Of String, String) = jss.Deserialize(
                Of Dictionary(Of String, String))(
                New JsonTextReader(New StringReader(apisetresponse)))
            If findDictionary.ContainsKey("error") Then
                MessageBox.Show(findDictionary("error"), "", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                If findDictionary("status") = "success" Then
                    fullname = searchDictionary("last_name") + ", " +
                                    searchDictionary("first_name") + " " +
                                    searchDictionary("middle_name").ElementAt(0) + "."
                    timearrived = DateTime.Now.ToString()
                    Dim participant_type As String
                    If searchDictionary("is_student") = "1" Then
                        participant_type = "Student"
                    Else
                        participant_type = "Professional"
                    End If
                    TextBox1.AppendText("Name: " + fullname + Environment.NewLine)
                        TextBox1.AppendText("Course: " + searchDictionary("course") + Environment.NewLine)
                        TextBox1.AppendText("School: " + searchDictionary("school") + Environment.NewLine)
                        TextBox1.AppendText("Email: " + searchDictionary("email") + Environment.NewLine)
                        TextBox1.AppendText("Contact: " + searchDictionary("contact") + Environment.NewLine)
                        TextBox1.AppendText("Date Registered: " + searchDictionary("created_at") + Environment.NewLine)
                    TextBox1.AppendText("Participant Type: " + participant_type)

                End If
                End If
        End If
        isBusy = False
    End Sub

End Class
