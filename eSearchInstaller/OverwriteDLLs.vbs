Option Explicit
Dim FSO, InstallDir, SourceDir
Set FSO = CreateObject("Scripting.FileSystemObject")
InstallDir = Session.Property("CustomActionData")
SourceDir = InstallDir & "\overwrite_dlls\"
If FSO.FileExists(SourceDir & "ggml-base.dll") Then
    FSO.CopyFile SourceDir & "ggml-base.dll", InstallDir & "\ggml-base.dll" , True
End If
If FSO.FileExists(SourceDir & "ggml-cpu.dll") Then
    FSO.CopyFile SourceDir & "ggml-cpu.dll", InstallDir & "\ggml-cpu.dll", True
End If
Set FSO = Nothing